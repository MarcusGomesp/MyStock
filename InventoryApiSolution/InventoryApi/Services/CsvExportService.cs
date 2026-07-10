using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using InventoryApi.Models;

namespace InventoryApi.Services;

/// <summary>
/// Exporta o inventário inteiro (todas as categorias juntas) para um único
/// CSV "achatado" — uma coluna "Categoria" e a união de todos os campos
/// possíveis, com célula vazia onde não se aplica.
///
/// Duas decisões deliberadas pra abrir certo ao dar duplo clique no Excel
/// em português:
///   - Delimitador ';' (o Excel pt-BR usa ',' como separador decimal, então
///     por padrão ele espera ';' entre colunas de CSV).
///   - BOM UTF-8 no início do arquivo, senão o Excel interpreta como
///     ANSI/Windows-1252 e todo acento vira caractere estranho.
///
/// Os nomes das colunas são os MESMOS que o importador de CSV reconhece
/// (ExcelHeaderReader.Normalize / InventoryItemBuilder), então o arquivo
/// exportado aqui pode ser reimportado depois sem nenhum ajuste manual.
/// </summary>
public class CsvExportService
{
    private static readonly string[] Headers =
    {
        "Categoria", "Unidade", "Andar", "Local", "Host", "Marca", "Modelo", "Patrimonio", "Serial number",
        "SSD/HD", "SO", "Memoria RAM", "Processador", "Monitores SN", "Monitor patrimonio",
        "Monitor modelo", "IP", "Nº de Serie", "Ramal", "Uso", "Item", "Quantidade", "Status"
    };

    public byte[] Export(List<InventoryItemBase> items)
    {
        using var stream = new MemoryStream();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";", NewLine = "\r\n" };

        // leaveOpen:true nos dois — sem isso, o CsvWriter fecha o StreamWriter
        // (e o StreamWriter fecha o MemoryStream) assim que o bloco 'using'
        // termina, e não dá mais pra ler os bytes depois.
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, config, leaveOpen: true))
        {
            foreach (var header in Headers)
                csv.WriteField(header);
            csv.NextRecord();

            foreach (var item in items)
            {
                WriteRow(csv, item);
                csv.NextRecord();
            }
        }

        return stream.ToArray();
    }

    private static void WriteRow(CsvWriter csv, InventoryItemBase item)
    {
        string? host = null, marca = null, modelo = null, patrimonio = null, serial = null, ssd = null,
            so = null, ram = null, proc = null, monSn = null, monPat = null, monModelo = null,
            ip = null, numeroSerie = null, ramal = null, uso = null, nomeItem = null, status = null, quantidade = null;

        switch (item)
        {
            case Computador c:
                host = c.Hostname; marca = c.Marca; modelo = c.Modelo; patrimonio = c.Patrimonio;
                serial = c.SerialNumber; ssd = c.SsdHd; so = c.SistemaOperacional; ram = c.MemoriaRam; proc = c.Processador;
                var monitor = c.Monitores.FirstOrDefault();
                if (monitor is not null) { monSn = monitor.NumeroSerie; monPat = monitor.Patrimonio; monModelo = monitor.Modelo; }
                break;

            case Impressora p:
                marca = p.Marca; modelo = p.Modelo; ip = p.Ip; numeroSerie = p.NumeroSerie; ramal = p.Ramal; status = p.Status;
                break;

            case ImpressoraTermica t:
                marca = t.Marca; modelo = t.Modelo; ip = t.Ip; numeroSerie = t.NumeroSerie; uso = t.Uso; status = t.Status;
                break;

            case MaterialEstoque m:
                nomeItem = m.Item; quantidade = m.Quantidade?.ToString(CultureInfo.InvariantCulture); status = m.Status; marca = m.Marca;
                break;
        }

        csv.WriteField(item.Categoria);
        csv.WriteField(item.Unidade ?? "");
        csv.WriteField(item.Andar ?? "");
        csv.WriteField(item.Local ?? "");
        csv.WriteField(host ?? "");
        csv.WriteField(marca ?? "");
        csv.WriteField(modelo ?? "");
        csv.WriteField(patrimonio ?? "");
        csv.WriteField(serial ?? "");
        csv.WriteField(ssd ?? "");
        csv.WriteField(so ?? "");
        csv.WriteField(ram ?? "");
        csv.WriteField(proc ?? "");
        csv.WriteField(monSn ?? "");
        csv.WriteField(monPat ?? "");
        csv.WriteField(monModelo ?? "");
        csv.WriteField(ip ?? "");
        csv.WriteField(numeroSerie ?? "");
        csv.WriteField(ramal ?? "");
        csv.WriteField(uso ?? "");
        csv.WriteField(nomeItem ?? "");
        csv.WriteField(quantidade ?? "");
        csv.WriteField(status ?? "");
    }
}
