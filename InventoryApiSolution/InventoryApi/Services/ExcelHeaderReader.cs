using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace InventoryApi.Services;

/// <summary>
/// Lê a linha de cabeçalho de uma planilha e permite buscar valores de célula
/// por qualquer um dos "apelidos" possíveis daquela coluna, sem diferenciar
/// maiúscula/minúscula ou acento. Isso é o que torna a importação tolerante
/// a pequenas variações como "Nº de Serie" vs "Serial number" vs "N° Serie".
/// </summary>
public class ExcelHeaderReader
{
    private readonly Dictionary<string, int> _headerToColumn = new();

    public ExcelHeaderReader(IXLRow headerRow)
    {
        foreach (var cell in headerRow.CellsUsed())
        {
            var normalized = Normalize(cell.GetString());
            if (!string.IsNullOrEmpty(normalized) && !_headerToColumn.ContainsKey(normalized))
                _headerToColumn[normalized] = cell.Address.ColumnNumber;
        }
    }

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var lowered = input.Trim().ToLowerInvariant();

        // remove acentos
        var normalized = lowered.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var result = sb.ToString().Normalize(NormalizationForm.FormC);

        // colapsa espaços e remove símbolos comuns (º, °, etc.)
        result = result.Replace("º", "").Replace("°", "").Replace(".", "").Replace(":", "");
        while (result.Contains("  ")) result = result.Replace("  ", " ");

        return result.Trim();
    }

    /// <summary>Retorna o valor da célula (como string) tentando cada nome possível de coluna, na ordem dada.</summary>
    public string? GetValue(IXLRow row, params string[] possibleHeaderNames)
    {
        foreach (var name in possibleHeaderNames)
        {
            var normalized = Normalize(name);
            if (_headerToColumn.TryGetValue(normalized, out var col))
            {
                var value = row.Cell(col).GetString().Trim();
                return string.IsNullOrWhiteSpace(value) || value == "-" ? null : value;
            }
        }
        return null;
    }

    public bool HasAnyColumn(params string[] possibleHeaderNames)
        => possibleHeaderNames.Any(n => _headerToColumn.ContainsKey(Normalize(n)));

    /// <summary>Cabeçalhos que existem na planilha mas não foram consumidos por nenhum campo mapeado.</summary>
    public Dictionary<string, string?> GetExtraFields(IXLRow row, HashSet<string> consumedNormalizedHeaders)
    {
        var extras = new Dictionary<string, string?>();
        foreach (var (header, col) in _headerToColumn)
        {
            if (consumedNormalizedHeaders.Contains(header)) continue;
            var value = row.Cell(col).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(value) && value != "-")
                extras[header] = value;
        }
        return extras;
    }

    public IEnumerable<string> ConsumedHeadersFor(params string[] possibleHeaderNames)
        => possibleHeaderNames.Select(Normalize).Where(_headerToColumn.ContainsKey);

    /// <summary>Vincula este leitor de cabeçalho a uma linha específica, expondo-a como IRowReader genérico.</summary>
    public IRowReader Bind(IXLRow row) => new ExcelRowReader(this, row);
}

internal class ExcelRowReader : IRowReader
{
    private readonly ExcelHeaderReader _headerReader;
    private readonly IXLRow _row;

    public ExcelRowReader(ExcelHeaderReader headerReader, IXLRow row)
    {
        _headerReader = headerReader;
        _row = row;
    }

    public string? GetValue(params string[] possibleHeaderNames) => _headerReader.GetValue(_row, possibleHeaderNames);
}
