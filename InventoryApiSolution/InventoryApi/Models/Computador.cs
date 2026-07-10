using MongoDB.Bson.Serialization.Attributes;

namespace InventoryApi.Models;

[BsonDiscriminator("Computador")]
public class Computador : InventoryItemBase
{
    public Computador() => Categoria = "Computador";

    public string? Hostname { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Patrimonio { get; set; }
    public string? SerialNumber { get; set; }
    public string? SsdHd { get; set; }
    public string? SistemaOperacional { get; set; }
    public string? MemoriaRam { get; set; }
    public string? Processador { get; set; }

    /// <summary>Um computador pode ter 0, 1 ou mais monitores.</summary>
    public List<MonitorInfo> Monitores { get; set; } = new();
}
