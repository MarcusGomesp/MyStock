using CsvHelper;

namespace InventoryApi.Services;

/// <summary>
/// Expõe a linha atual de um CsvReader (CsvHelper) como IRowReader, usando o
/// mesmo casamento flexível de cabeçalho (sem acento/maiúscula) do Excel,
/// através do ExcelHeaderReader.Normalize compartilhado.
/// </summary>
public class CsvRowReader : IRowReader
{
    private readonly CsvReader _csv;
    private readonly Dictionary<string, int> _headerIndex;

    public CsvRowReader(CsvReader csv, Dictionary<string, int> headerIndex)
    {
        _csv = csv;
        _headerIndex = headerIndex;
    }

    /// <summary>Constrói o índice normalizado de cabeçalho a partir da linha de header do CSV.</summary>
    public static Dictionary<string, int> BuildHeaderIndex(string[] headerRecord)
    {
        var dict = new Dictionary<string, int>();
        for (int i = 0; i < headerRecord.Length; i++)
        {
            var normalized = ExcelHeaderReader.Normalize(headerRecord[i]);
            if (!string.IsNullOrEmpty(normalized) && !dict.ContainsKey(normalized))
                dict[normalized] = i;
        }
        return dict;
    }

    public string? GetValue(params string[] possibleHeaderNames)
    {
        foreach (var name in possibleHeaderNames)
        {
            var normalized = ExcelHeaderReader.Normalize(name);
            if (_headerIndex.TryGetValue(normalized, out var index))
            {
                var value = _csv.GetField(index)?.Trim();
                return string.IsNullOrWhiteSpace(value) || value == "-" ? null : value;
            }
        }
        return null;
    }
}
