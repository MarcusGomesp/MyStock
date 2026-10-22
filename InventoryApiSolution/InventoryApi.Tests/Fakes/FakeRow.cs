using InventoryApi.Services;

namespace InventoryApi.Tests.Fakes;

/// <summary>
/// Linha de planilha em memória para os testes. Casa o cabeçalho do mesmo jeito
/// que os leitores reais (sem acento, sem diferenciar maiúscula/minúscula).
/// </summary>
public class FakeRow : IRowReader
{
    private readonly Dictionary<string, string?> _values = new();

    public FakeRow(params (string Header, string? Value)[] cells)
    {
        foreach (var (header, value) in cells)
            _values[ExcelHeaderReader.Normalize(header)] = value;
    }

    public string? GetValue(params string[] possibleHeaderNames)
    {
        foreach (var name in possibleHeaderNames)
            if (_values.TryGetValue(ExcelHeaderReader.Normalize(name), out var value))
                return string.IsNullOrWhiteSpace(value) ? null : value;
        return null;
    }
}
