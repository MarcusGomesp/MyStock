using InventoryApi.Services;

namespace InventoryApi.Tests;

/// <summary>
/// O Normalize é o que permite reconhecer "Memória RAM", "MEMORIA RAM" e
/// "memoria  ram" como a mesma coluna — base de toda a importação.
/// </summary>
public class ExcelHeaderReaderNormalizeTests
{
    [Theory]
    [InlineData("  Memória RAM ", "memoria ram")]
    [InlineData("PATRIMÔNIO", "patrimonio")]
    [InlineData("Nº de Série", "n de serie")]
    [InlineData("Monitores  SN", "monitores sn")]
    [InlineData("SSD/HD", "ssd/hd")]
    [InlineData("Andar:", "andar")]
    public void Normalize_IgnoraAcentoMaiusculaEspacosESimbolos(string entrada, string esperado)
    {
        Assert.Equal(esperado, ExcelHeaderReader.Normalize(entrada));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_TextoVazio_RetornaVazio(string entrada)
    {
        Assert.Equal(string.Empty, ExcelHeaderReader.Normalize(entrada));
    }
}
