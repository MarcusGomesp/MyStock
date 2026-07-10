namespace InventoryApi.Services;

/// <summary>
/// Abstração de "uma linha de dados com cabeçalho nomeado", implementada tanto
/// para Excel (ExcelRowReader) quanto para CSV (CsvRowReader). Isso permite que
/// a lógica de "qual categoria vira qual objeto C#" (InventoryItemBuilder) seja
/// escrita uma única vez e reaproveitada nos dois formatos de arquivo.
/// </summary>
public interface IRowReader
{
    /// <summary>Busca o valor da célula/coluna tentando cada nome possível de cabeçalho, em ordem.</summary>
    string? GetValue(params string[] possibleHeaderNames);
}
