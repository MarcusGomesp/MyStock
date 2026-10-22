using System.Text;
using InventoryApi.Models;
using InventoryApi.Services;
using InventoryApi.Tests.Fakes;

namespace InventoryApi.Tests;

public class CsvImportServiceTests
{
    private readonly FakeInventoryService _inventario = new();
    private readonly FakeActivityLogService _log = new();
    private readonly CsvImportService _servico;

    public CsvImportServiceTests()
    {
        _servico = new CsvImportService(_inventario, _log);
    }

    private static Stream Csv(string conteudo) => new MemoryStream(Encoding.UTF8.GetBytes(conteudo));

    [Fact]
    public async Task ImportAsync_CsvValido_ImportaItensNaUnidadeDoNomeDoArquivo()
    {
        var csv = "Categoria,Host,Marca\nComputador,PC-01,Dell\nNotebook,NB-02,Lenovo\n";

        var resultado = await _servico.ImportAsync(Csv(csv), "Unidade Roma.csv", categoriaPadrao: null);

        Assert.Equal("Roma", resultado.Unidade);
        Assert.Equal(2, resultado.TotalItensImportados);
        Assert.Equal(2, _inventario.Itens.Count);
        Assert.All(_inventario.Itens, i => Assert.Equal("Roma", i.Unidade));
        Assert.All(_inventario.Itens, i => Assert.Equal("Unidade Roma.csv", i.Origem));
        Assert.True(resultado.Planilhas[0].ColunasDetectadas["Host"]);
        Assert.False(resultado.Planilhas[0].ColunasDetectadas["IP"]);
    }

    [Fact]
    public async Task ImportAsync_Reimportacao_SubstituiOsItensDaUnidadeSemDuplicar()
    {
        var csv = "Categoria,Host\nComputador,PC-01\n";

        await _servico.ImportAsync(Csv(csv), "Unidade Roma.csv", null);
        var segunda = await _servico.ImportAsync(Csv(csv), "Unidade Roma.csv", null);

        Assert.Single(_inventario.Itens);
        Assert.Equal(1, segunda.ItensSubstituidos);
    }

    [Fact]
    public async Task ImportAsync_SemColunaCategoria_UsaACategoriaPadrao()
    {
        var csv = "Marca,Modelo,IP\nHP,M404,10.0.0.5\n";

        var resultado = await _servico.ImportAsync(Csv(csv), "Matriz.csv", categoriaPadrao: "Impressora");

        var impressora = Assert.IsType<Impressora>(Assert.Single(_inventario.Itens));
        Assert.Equal("10.0.0.5", impressora.Ip);
        Assert.Equal(1, resultado.TotalItensImportados);
    }

    [Fact]
    public async Task ImportAsync_LinhaSemCategoria_EIgnoradaComAviso()
    {
        var csv = "Marca,Modelo\nHP,M404\n";

        var resultado = await _servico.ImportAsync(Csv(csv), "Matriz.csv", categoriaPadrao: null);

        var planilha = Assert.Single(resultado.Planilhas);
        Assert.Equal(1, planilha.LinhasIgnoradas);
        Assert.Contains(planilha.Avisos, a => a.Contains("categoria não informada"));
        Assert.Empty(_inventario.Itens);
    }

    [Fact]
    public async Task ImportAsync_CategoriaDesconhecida_EIgnoradaComAviso()
    {
        var csv = "Categoria,Marca\nGeladeira,Brastemp\nComputador,Dell\n";

        var resultado = await _servico.ImportAsync(Csv(csv), "Matriz.csv", null);

        Assert.Equal(1, resultado.TotalItensImportados);
        Assert.Contains(resultado.Planilhas[0].Avisos, a => a.Contains("'Geladeira' não reconhecida"));
    }

    [Fact]
    public async Task ImportAsync_LinhaDeTituloAntesDoCabecalho_EDetectadaEIgnorada()
    {
        var csv = "IMPRESSORAS EM USO,,\nMarca,Modelo,IP\nHP,M404,10.0.0.5\n";

        var resultado = await _servico.ImportAsync(Csv(csv), "Unidade Roma.csv", categoriaPadrao: "Impressora");

        Assert.Equal(1, resultado.TotalItensImportados);
        Assert.Contains(resultado.Planilhas[0].Avisos, a => a.Contains("IMPRESSORAS EM USO"));
    }

    [Fact]
    public async Task ImportAsync_ArquivoVazio_NaoApagaOsDadosExistentesDaUnidade()
    {
        _inventario.Itens.Add(new Computador { Unidade = "Roma", Hostname = "PC-ANTIGO" });

        var resultado = await _servico.ImportAsync(Csv(""), "Unidade Roma.csv", null);

        Assert.Single(_inventario.Itens);
        Assert.Empty(_inventario.UnidadesApagadas);
        Assert.Equal(0, _inventario.ChamadasCreateMany);
        Assert.Contains(resultado.Planilhas[0].Avisos, a => a.Contains("vazio"));
    }

    [Fact]
    public async Task ImportAsync_SemItensValidos_MantemOsDadosExistentes()
    {
        _inventario.Itens.Add(new Computador { Unidade = "Roma", Hostname = "PC-ANTIGO" });
        var csv = "Categoria,Marca\nGeladeira,Brastemp\n";

        await _servico.ImportAsync(Csv(csv), "Unidade Roma.csv", null);

        Assert.Single(_inventario.Itens);
        Assert.Empty(_inventario.UnidadesApagadas);
    }

    [Fact]
    public async Task ImportAsync_RegistraAImportacaoNoHistorico()
    {
        await _servico.ImportAsync(Csv("Categoria,Host\nComputador,PC-01\n"), "Matriz.csv", null);

        Assert.Equal(new[] { "ImportarCsv" }, _log.Acoes);
    }
}
