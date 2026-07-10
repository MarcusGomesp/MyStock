using InventoryApi.Models;

namespace InventoryApi.Services;

/// <summary>
/// Converte uma linha genérica (IRowReader) — venha ela de uma planilha Excel
/// ou de um CSV — no objeto tipado correto (Computador, Impressora, etc).
/// Essa é a ÚNICA lógica de mapeamento de campo; tanto ExcelImportService
/// quanto CsvImportService chamam este builder.
/// </summary>
public static class InventoryItemBuilder
{
    public static InventoryItemBase? BuildItem(string categoria, IRowReader row)
    {
        InventoryItemBase? item = categoria.Trim().ToLowerInvariant() switch
        {
            "computador" or "notebook" or "desktop" => BuildComputador(row, categoria),
            "impressora" => BuildImpressora(row),
            "impressoratermica" or "etiquetadora" or "impressora termica" => BuildImpressoraTermica(row),
            "materialestoque" or "material" or "estoque" => BuildMaterialEstoque(row),
            _ => null
        };

        if (item is not null)
        {
            item.Andar ??= row.GetValue("Andar");
            item.Local ??= row.GetValue("Local");
        }

        return item;
    }

    private static Computador BuildComputador(IRowReader row, string categoria)
    {
        var c = new Computador
        {
            Categoria = char.ToUpperInvariant(categoria[0]) + categoria[1..].ToLowerInvariant(),
            Hostname = row.GetValue("Host", "Hostname"),
            Marca = row.GetValue("Marca"),
            Modelo = row.GetValue("Modelo"),
            Patrimonio = row.GetValue("Patrimonio", "Patrimônio"),
            SerialNumber = row.GetValue("Serial number", "Serial Number", "Numero de serie", "Nº de Serie"),
            SsdHd = row.GetValue("SSD/HD", "SSD", "HD"),
            SistemaOperacional = row.GetValue("SO", "Sistema Operacional"),
            MemoriaRam = row.GetValue("Memoria RAM", "Memória RAM", "RAM"),
            Processador = row.GetValue("Processador", "CPU"),
        };

        var monitorSn = row.GetValue("Monitores SN", "Monitor SN", "Monitores  SN");
        var monitorPatrimonio = row.GetValue("Monitor patrimonio", "Monitor Patrimônio");
        var monitorModelo = row.GetValue("Monitor modelo", "Monitor Modelo");

        if (monitorSn is not null || monitorPatrimonio is not null || monitorModelo is not null)
        {
            c.Monitores.Add(new MonitorInfo
            {
                NumeroSerie = monitorSn,
                Patrimonio = monitorPatrimonio,
                Modelo = monitorModelo
            });
        }

        return c;
    }

    private static Impressora BuildImpressora(IRowReader row)
    {
        return new Impressora
        {
            Marca = row.GetValue("Marca"),
            Modelo = row.GetValue("Modelo"),
            Ip = row.GetValue("IP"),
            NumeroSerie = row.GetValue("Nº de Serie", "N de Serie", "Numero de Serie", "Serial number"),
            Ramal = row.GetValue("Ramal"),
            Status = row.GetValue("Status", "Funcional"),
            Observacao = row.GetValue("Observacao", "Observação", "Column2")
        };
    }

    private static ImpressoraTermica BuildImpressoraTermica(IRowReader row)
    {
        return new ImpressoraTermica
        {
            Marca = row.GetValue("Marca"),
            Modelo = row.GetValue("Modelo"),
            Ip = row.GetValue("IP"),
            NumeroSerie = row.GetValue("Nº de Serie", "N de Serie", "Numero de Serie"),
            Uso = row.GetValue("Uso"),
            Status = row.GetValue("Status")
        };
    }

    private static MaterialEstoque BuildMaterialEstoque(IRowReader row)
    {
        var qtdStr = row.GetValue("Quantidade");
        int? qtd = int.TryParse(qtdStr, out var parsed) ? parsed : null;

        return new MaterialEstoque
        {
            Item = row.GetValue("Item"),
            Quantidade = qtd,
            Status = row.GetValue("Status"),
            Marca = row.GetValue("Marca")
        };
    }
}
