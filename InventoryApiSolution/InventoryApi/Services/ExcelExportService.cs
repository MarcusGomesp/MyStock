using ClosedXML.Excel;
using InventoryApi.Models;

namespace InventoryApi.Services;

public class ExcelExportService
{
    public byte[] Export(List<InventoryItemBase> items)
    {
        using var workbook = new XLWorkbook();

        var computadores = items.OfType<Computador>().ToList();
        var impressoras = items.OfType<Impressora>().ToList();
        var impressorasTermicas = items.OfType<ImpressoraTermica>().ToList();
        var materiais = items.OfType<MaterialEstoque>().ToList();

        if (computadores.Count > 0) WriteComputadores(workbook, computadores);
        if (impressoras.Count > 0) WriteImpressoras(workbook, impressoras);
        if (impressorasTermicas.Count > 0) WriteImpressorasTermicas(workbook, impressorasTermicas);
        if (materiais.Count > 0) WriteMateriais(workbook, materiais);

        if (workbook.Worksheets.Count == 0)
            workbook.AddWorksheet("Sem dados");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void WriteComputadores(XLWorkbook wb, List<Computador> items)
    {
        var ws = wb.AddWorksheet("Computadores");
        string[] headers = { "Unidade", "Andar", "Host", "Marca", "Modelo", "Patrimonio", "Serial Number", "SSD/HD", "SO",
            "Memoria RAM", "Processador", "Monitor SN", "Monitor Patrimonio", "Monitor Modelo", "Local" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var c in items)
        {
            var monitor = c.Monitores.FirstOrDefault();
            ws.Cell(row, 1).Value = c.Unidade;
            ws.Cell(row, 2).Value = c.Andar;
            ws.Cell(row, 3).Value = c.Hostname;
            ws.Cell(row, 4).Value = c.Marca;
            ws.Cell(row, 5).Value = c.Modelo;
            ws.Cell(row, 6).Value = c.Patrimonio;
            ws.Cell(row, 7).Value = c.SerialNumber;
            ws.Cell(row, 8).Value = c.SsdHd;
            ws.Cell(row, 9).Value = c.SistemaOperacional;
            ws.Cell(row, 10).Value = c.MemoriaRam;
            ws.Cell(row, 11).Value = c.Processador;
            ws.Cell(row, 12).Value = monitor?.NumeroSerie;
            ws.Cell(row, 13).Value = monitor?.Patrimonio;
            ws.Cell(row, 14).Value = monitor?.Modelo;
            ws.Cell(row, 15).Value = c.Local;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private void WriteImpressoras(XLWorkbook wb, List<Impressora> items)
    {
        var ws = wb.AddWorksheet("Impressoras");
        string[] headers = { "Unidade", "Andar", "IP", "Marca", "Modelo", "Nº de Serie", "Local", "Ramal", "Status" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var p in items)
        {
            ws.Cell(row, 1).Value = p.Unidade;
            ws.Cell(row, 2).Value = p.Andar;
            ws.Cell(row, 3).Value = p.Ip;
            ws.Cell(row, 4).Value = p.Marca;
            ws.Cell(row, 5).Value = p.Modelo;
            ws.Cell(row, 6).Value = p.NumeroSerie;
            ws.Cell(row, 7).Value = p.Local;
            ws.Cell(row, 8).Value = p.Ramal;
            ws.Cell(row, 9).Value = p.Status;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private void WriteImpressorasTermicas(XLWorkbook wb, List<ImpressoraTermica> items)
    {
        var ws = wb.AddWorksheet("Etiquetadoras");
        string[] headers = { "Unidade", "Andar", "IP", "Uso", "Modelo", "Nº de Serie", "Local", "Status" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var e in items)
        {
            ws.Cell(row, 1).Value = e.Unidade;
            ws.Cell(row, 2).Value = e.Andar;
            ws.Cell(row, 3).Value = e.Ip;
            ws.Cell(row, 4).Value = e.Uso;
            ws.Cell(row, 5).Value = e.Modelo;
            ws.Cell(row, 6).Value = e.NumeroSerie;
            ws.Cell(row, 7).Value = e.Local;
            ws.Cell(row, 8).Value = e.Status;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private void WriteMateriais(XLWorkbook wb, List<MaterialEstoque> items)
    {
        var ws = wb.AddWorksheet("Materiais de Estoque");
        string[] headers = { "Unidade", "Item", "Quantidade", "Status", "Marca" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var m in items)
        {
            ws.Cell(row, 1).Value = m.Unidade;
            ws.Cell(row, 2).Value = m.Item;
            ws.Cell(row, 3).Value = m.Quantidade;
            ws.Cell(row, 4).Value = m.Status;
            ws.Cell(row, 5).Value = m.Marca;
            row++;
        }
        ws.Columns().AdjustToContents();
    }
}
