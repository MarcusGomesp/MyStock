using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using InventoryApi.DTOs;
using InventoryApi.Models;

namespace InventoryApi.Services;

/// <summary>
/// Importa um arquivo .csv (uma tabela só, sem abas). Como não existe "nome da
/// aba" para descobrir a categoria automaticamente, a categoria vem de:
///   1) uma coluna "Categoria" na própria linha (maior prioridade), ou
///   2) o parâmetro categoriaPadrao (ex: passado pelo front-end via um
///      seletor "qual tipo de equipamento é esse arquivo?").
/// Se nenhum dos dois existir, a linha é ignorada e reportada em Avisos.
/// </summary>
public class CsvImportService
{
    private readonly IInventoryService _inventoryService;
    private readonly IActivityLogService _activityLog;

    public CsvImportService(IInventoryService inventoryService, IActivityLogService activityLog)
    {
        _inventoryService = inventoryService;
        _activityLog = activityLog;
    }

    public async Task<ImportResultDto> ImportAsync(Stream fileStream, string fileName, string? categoriaPadrao, string? unidadeOverride = null)
    {
        var unidade = string.IsNullOrWhiteSpace(unidadeOverride)
            ? UnidadeHelper.ExtractFromFileName(fileName)
            : unidadeOverride.Trim();

        var result = new ImportResultDto { Arquivo = fileName, Unidade = unidade };
        var sheetResult = new SheetImportResultDto
        {
            NomePlanilha = fileName,
            CategoriaPadrao = categoriaPadrao ?? "(detectar por linha via coluna 'Categoria')"
        };

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,   // não falha se uma linha tiver menos colunas que o cabeçalho
            BadDataFound = null,        // não falha em campo mal-formado (ex: aspas soltas)
            DetectDelimiter = true,     // detecta automaticamente ',' ou ';' (comum em CSV exportado de Excel pt-BR)
        };

        // UTF-8 com detecção de BOM: cobre a maioria dos arquivos gerados hoje em dia.
        // Se o CSV vier de um sistema legado em ANSI/Windows-1252, acentos podem
        // aparecer trocados — nesse caso, reabra e salve o CSV como UTF-8 antes de importar.
        using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, config);

        if (!await csv.ReadAsync() || !csv.ReadHeader() || csv.HeaderRecord is null)
        {
            sheetResult.Avisos.Add("Arquivo CSV vazio ou sem linha de cabeçalho. Nenhum dado da unidade foi alterado.");
            result.Planilhas.Add(sheetResult);
            return result;
        }

        // Algumas planilhas (como as suas de "IMPRESSORAS EM USO", "ETIQUETADORAS
        // EM USO" etc) têm um título numa linha própria, com só a primeira célula
        // preenchida, e o cabeçalho de verdade (ANDAR, IP, MARCA...) só na linha
        // seguinte. Se exportado pra CSV, essa linha de título vira a "linha 1"
        // e seria erroneamente tratada como cabeçalho — sem isso, NENHUMA coluna
        // seria reconhecida. Detectamos isso (linha com quase tudo vazio) e
        // avançamos pra próxima linha como cabeçalho real, igual já acontecia
        // no import de Excel.
        var nonEmptyHeaderCells = csv.HeaderRecord.Count(h => !string.IsNullOrWhiteSpace(h));
        if (nonEmptyHeaderCells <= 1)
        {
            var tituloDetectado = csv.HeaderRecord.FirstOrDefault(h => !string.IsNullOrWhiteSpace(h));
            if (!await csv.ReadAsync() || !csv.ReadHeader() || csv.HeaderRecord is null)
            {
                sheetResult.Avisos.Add("Arquivo CSV sem linha de cabeçalho válida após o título. Nenhum dado da unidade foi alterado.");
                result.Planilhas.Add(sheetResult);
                return result;
            }
            sheetResult.Avisos.Add($"Linha de título \"{tituloDetectado}\" detectada e ignorada — usando a linha seguinte como cabeçalho.");
        }

        var headerIndex = CsvRowReader.BuildHeaderIndex(csv.HeaderRecord);

        sheetResult.ColunasDetectadas = FieldAliasCatalog.Aliases.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Any(alias => headerIndex.ContainsKey(ExcelHeaderReader.Normalize(alias))));

        var itemsToInsert = new List<InventoryItemBase>();
        var linhaAtual = 1; // linha 1 = cabeçalho

        while (await csv.ReadAsync())
        {
            linhaAtual++;

            var rowReader = new CsvRowReader(csv, headerIndex);
            var categoriaLinha = rowReader.GetValue("Categoria") ?? categoriaPadrao;

            if (string.IsNullOrWhiteSpace(categoriaLinha))
            {
                sheetResult.LinhasIgnoradas++;
                sheetResult.Avisos.Add($"Linha {linhaAtual}: categoria não informada (adicione uma coluna 'Categoria' no CSV ou selecione a categoria padrão do arquivo antes de importar).");
                continue;
            }

            var item = InventoryItemBuilder.BuildItem(categoriaLinha, rowReader);
            if (item is null)
            {
                sheetResult.LinhasIgnoradas++;
                sheetResult.Avisos.Add($"Linha {linhaAtual}: categoria '{categoriaLinha}' não reconhecida.");
                continue;
            }

            item.Origem = fileName;
            item.PlanilhaOrigem = fileName;
            item.Unidade = rowReader.GetValue("Unidade") ?? unidade;
            itemsToInsert.Add(item);
        }

        if (itemsToInsert.Count > 0)
        {
            // Sincroniza por unidade: normalmente todas as linhas são da mesma
            // unidade (o caso comum: 1 arquivo = 1 unidade), mas se o CSV tiver
            // uma coluna "Unidade" preenchida linha a linha (ex: reimportação de
            // um export combinado), cada unidade encontrada é sincronizada
            // separadamente, sem misturar itens de unidades diferentes.
            var unidadesEnvolvidas = itemsToInsert
                .Select(i => i.Unidade)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            long totalSubstituidos = 0;
            foreach (var u in unidadesEnvolvidas)
                totalSubstituidos += await _inventoryService.DeleteByUnidadeAsync(u!);

            result.ItensSubstituidos = totalSubstituidos;
            await _inventoryService.CreateManyAsync(itemsToInsert);
        }
        else
        {
            sheetResult.Avisos.Add($"Nenhum item válido encontrado no arquivo — os dados já existentes da unidade '{unidade}' foram mantidos sem alteração.");
        }

        sheetResult.ItensImportados = itemsToInsert.Count;
        result.Planilhas.Add(sheetResult);

        await _activityLog.LogAsync("ImportarCsv", categoriaPadrao, unidade, null,
            $"{fileName}: {itemsToInsert.Count} item(ns) importado(s), {result.ItensSubstituidos} substituído(s).");

        return result;
    }
}
