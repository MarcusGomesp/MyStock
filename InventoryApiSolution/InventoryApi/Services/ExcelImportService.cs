using ClosedXML.Excel;
using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using Microsoft.Extensions.Options;

namespace InventoryApi.Services;

public class ExcelImportService
{
    private readonly IInventoryService _inventoryService;
    private readonly SheetMappingSettings _sheetMappings;
    private readonly IActivityLogService _activityLog;

    public ExcelImportService(IInventoryService inventoryService, IOptions<SheetMappingSettings> sheetMappings, IActivityLogService activityLog)
    {
        _inventoryService = inventoryService;
        _sheetMappings = sheetMappings.Value;
        _activityLog = activityLog;
    }

    public async Task<ImportResultDto> ImportAsync(Stream fileStream, string fileName, string? unidadeOverride = null)
    {
        var unidade = string.IsNullOrWhiteSpace(unidadeOverride)
            ? UnidadeHelper.ExtractFromFileName(fileName)
            : unidadeOverride.Trim();

        var result = new ImportResultDto { Arquivo = fileName, Unidade = unidade };
        var todosOsItens = new List<InventoryItemBase>();

        using var workbook = new XLWorkbook(fileStream);

        foreach (var worksheet in workbook.Worksheets)
        {
            var firstRow = worksheet.RowsUsed().FirstOrDefault();
            if (firstRow is null) continue;

            // Algumas planilhas têm um título mesclado na linha 1 (ex: "IMPRESSORAS EM USO")
            // e o cabeçalho de verdade só na linha 2. Detectamos isso: se a linha 1 tiver
            // poucas células preenchidas em relação ao total de colunas usadas, pulamos pra próxima.
            var headerRow = firstRow;
            if (headerRow.CellsUsed().Count() <= 1 && worksheet.RowsUsed().Count() > 1)
                headerRow = worksheet.RowsUsed().Skip(1).First();

            var sheetResult = new SheetImportResultDto { NomePlanilha = worksheet.Name };

            var categoriaPadrao = ResolveCategoriaPadrao(worksheet.Name);
            sheetResult.CategoriaPadrao = categoriaPadrao ?? "(detectar por linha)";

            var headerReader = new ExcelHeaderReader(headerRow);

            sheetResult.ColunasDetectadas = FieldAliasCatalog.Aliases.ToDictionary(
                kv => kv.Key,
                kv => headerReader.HasAnyColumn(kv.Value));

            var itemsDaAba = new List<InventoryItemBase>();

            var dataRows = worksheet.RowsUsed().Where(r => r.RowNumber() > headerRow.RowNumber());

            foreach (var row in dataRows)
            {
                if (row.CellsUsed().Count() == 0) continue;

                var rowReader = headerReader.Bind(row);
                var categoriaLinha = rowReader.GetValue("Categoria") ?? categoriaPadrao;

                if (string.IsNullOrWhiteSpace(categoriaLinha))
                {
                    sheetResult.LinhasIgnoradas++;
                    sheetResult.Avisos.Add($"Linha {row.RowNumber()}: não foi possível determinar a categoria (adicione uma coluna 'Categoria' ou configure o mapeamento da aba '{worksheet.Name}').");
                    continue;
                }

                var item = InventoryItemBuilder.BuildItem(categoriaLinha, rowReader);
                if (item is null)
                {
                    sheetResult.LinhasIgnoradas++;
                    sheetResult.Avisos.Add($"Linha {row.RowNumber()}: categoria '{categoriaLinha}' não reconhecida.");
                    continue;
                }

                item.Origem = fileName;
                item.PlanilhaOrigem = worksheet.Name;
                item.Unidade = rowReader.GetValue("Unidade") ?? unidade;
                itemsDaAba.Add(item);
            }

            sheetResult.ItensImportados = itemsDaAba.Count;
            result.Planilhas.Add(sheetResult);
            todosOsItens.AddRange(itemsDaAba);
        }

        if (todosOsItens.Count > 0)
        {
            // Sincroniza por unidade envolvida (normalmente uma só, o arquivo
            // inteiro — mas se alguma linha tiver coluna "Unidade" própria,
            // cada unidade encontrada é sincronizada separadamente).
            var unidadesEnvolvidas = todosOsItens
                .Select(i => i.Unidade)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            long totalSubstituidos = 0;
            foreach (var u in unidadesEnvolvidas)
                totalSubstituidos += await _inventoryService.DeleteByUnidadeAsync(u!);

            result.ItensSubstituidos = totalSubstituidos;
            await _inventoryService.CreateManyAsync(todosOsItens);
        }
        else if (result.Planilhas.Count > 0)
        {
            result.Planilhas[0].Avisos.Add($"Nenhum item válido encontrado no arquivo — os dados já existentes da unidade '{unidade}' foram mantidos sem alteração.");
        }

        await _activityLog.LogAsync("ImportarExcel", null, unidade, null,
            $"{fileName}: {todosOsItens.Count} item(ns) importado(s), {result.ItensSubstituidos} substituído(s).");

        return result;
    }

    private string? ResolveCategoriaPadrao(string sheetName)
    {
        // match exato ou por aproximação (contains), ignorando maiúsculas/minúsculas
        if (_sheetMappings.Mappings.TryGetValue(sheetName, out var exact))
            return exact;

        var match = _sheetMappings.Mappings.Keys
            .FirstOrDefault(k => sheetName.Contains(k, StringComparison.OrdinalIgnoreCase)
                               || k.Contains(sheetName, StringComparison.OrdinalIgnoreCase));

        return match is null ? null : _sheetMappings.Mappings[match];
    }
}
