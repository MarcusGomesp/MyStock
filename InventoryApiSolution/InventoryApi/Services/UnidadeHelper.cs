namespace InventoryApi.Services;

/// <summary>
/// Extrai o nome da "Unidade" a partir do nome do arquivo importado.
/// Ex: "Unidade Roma.csv" -> "Roma", "unidade_alemanha.xlsx" -> "alemanha",
/// "Matriz.csv" -> "Matriz" (sem prefixo reconhecido, usa o nome inteiro).
/// </summary>
public static class UnidadeHelper
{
    private static readonly string[] Prefixos = { "unidade ", "unidade_", "unidade-", "unidade" };

    public static string ExtractFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return "SemUnidade";

        var lower = name.ToLowerInvariant();
        foreach (var prefixo in Prefixos)
        {
            if (lower.StartsWith(prefixo, StringComparison.Ordinal))
            {
                var resto = name[prefixo.Length..].Trim(' ', '_', '-');
                return string.IsNullOrWhiteSpace(resto) ? name : resto;
            }
        }

        return name;
    }
}
