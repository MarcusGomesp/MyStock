using InventoryApi.Models;
using InventoryApi.Services;
using InventoryApi.Tests.Fakes;

namespace InventoryApi.Tests;

public class InventoryItemBuilderTests
{
    [Fact]
    public void BuildItem_Notebook_CriaComputadorUsandoApelidosDeColuna()
    {
        var linha = new FakeRow(
            ("Hostname", "NB-0042"),
            ("Memória RAM", "16 GB"),
            ("CPU", "i5-1235U"),
            ("Patrimônio", "PAT-77"),
            ("Andar", "17º"),
            ("Local", "Financeiro"));

        var item = InventoryItemBuilder.BuildItem("notebook", linha);

        var pc = Assert.IsType<Computador>(item);
        Assert.Equal("Notebook", pc.Categoria);
        Assert.Equal("NB-0042", pc.Hostname);
        Assert.Equal("16 GB", pc.MemoriaRam);
        Assert.Equal("i5-1235U", pc.Processador);
        Assert.Equal("PAT-77", pc.Patrimonio);
        Assert.Equal("17º", pc.Andar);
        Assert.Equal("Financeiro", pc.Local);
    }

    [Fact]
    public void BuildItem_ComputadorComDadosDeMonitor_AdicionaMonitor()
    {
        var linha = new FakeRow(("Host", "PC-01"), ("Monitor SN", "MON123"), ("Monitor Patrimônio", "PAT-9"));

        var pc = Assert.IsType<Computador>(InventoryItemBuilder.BuildItem("Computador", linha));

        var monitor = Assert.Single(pc.Monitores);
        Assert.Equal("MON123", monitor.NumeroSerie);
        Assert.Equal("PAT-9", monitor.Patrimonio);
    }

    [Fact]
    public void BuildItem_ComputadorSemDadosDeMonitor_NaoCriaMonitorVazio()
    {
        var pc = Assert.IsType<Computador>(InventoryItemBuilder.BuildItem("Desktop", new FakeRow(("Host", "PC-02"))));

        Assert.Empty(pc.Monitores);
    }

    [Fact]
    public void BuildItem_Impressora_PreencheIpESerie()
    {
        var linha = new FakeRow(("IP", "10.0.0.5"), ("Nº de Serie", "SN-555"), ("Marca", "HP"));

        var impressora = Assert.IsType<Impressora>(InventoryItemBuilder.BuildItem(" Impressora ", linha));

        Assert.Equal("10.0.0.5", impressora.Ip);
        Assert.Equal("SN-555", impressora.NumeroSerie);
        Assert.Equal("HP", impressora.Marca);
    }

    [Theory]
    [InlineData("Etiquetadora")]
    [InlineData("impressora termica")]
    [InlineData("ImpressoraTermica")]
    public void BuildItem_SinonimosDeEtiquetadora_CriamImpressoraTermica(string categoria)
    {
        Assert.IsType<ImpressoraTermica>(InventoryItemBuilder.BuildItem(categoria, new FakeRow(("Marca", "Zebra"))));
    }

    [Theory]
    [InlineData("12", 12)]
    [InlineData("doze", null)]
    [InlineData(null, null)]
    public void BuildItem_MaterialEstoque_ConverteQuantidade(string? quantidade, int? esperado)
    {
        var linha = new FakeRow(("Item", "Cabo HDMI"), ("Quantidade", quantidade));

        var material = Assert.IsType<MaterialEstoque>(InventoryItemBuilder.BuildItem("estoque", linha));

        Assert.Equal("Cabo HDMI", material.Item);
        Assert.Equal(esperado, material.Quantidade);
    }

    [Fact]
    public void BuildItem_CategoriaDesconhecida_RetornaNull()
    {
        Assert.Null(InventoryItemBuilder.BuildItem("Geladeira", new FakeRow(("Marca", "X"))));
    }
}
