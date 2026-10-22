using InventoryApi.Services;

namespace InventoryApi.Tests;

public class UnidadeHelperTests
{
    [Theory]
    [InlineData("Unidade Roma.csv", "Roma")]
    [InlineData("unidade_alemanha.xlsx", "alemanha")]
    [InlineData("Unidade-Paris.csv", "Paris")]
    [InlineData("UnidadeLondres.xlsx", "Londres")]
    [InlineData("Matriz.csv", "Matriz")]
    public void ExtractFromFileName_RemoveOPrefixoUnidade(string arquivo, string esperado)
    {
        Assert.Equal(esperado, UnidadeHelper.ExtractFromFileName(arquivo));
    }

    [Fact]
    public void ExtractFromFileName_SemNome_RetornaSemUnidade()
    {
        Assert.Equal("SemUnidade", UnidadeHelper.ExtractFromFileName(".csv"));
    }

    [Fact]
    public void ExtractFromFileName_SoOPrefixo_MantemONomeOriginal()
    {
        Assert.Equal("Unidade", UnidadeHelper.ExtractFromFileName("Unidade.csv"));
    }
}
