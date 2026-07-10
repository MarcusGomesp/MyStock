namespace InventoryApi.Models;

/// <summary>
/// Monitor é um objeto embutido (não tem coleção própria), pois vive
/// dentro do documento do Computador ao qual pertence. Um computador
/// pode ter mais de um monitor (por isso é uma lista).
/// </summary>
public class MonitorInfo
{
    public string? NumeroSerie { get; set; }
    public string? Patrimonio { get; set; }
    public string? Modelo { get; set; }
}
