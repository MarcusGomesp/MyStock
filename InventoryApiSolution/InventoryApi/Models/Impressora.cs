using MongoDB.Bson.Serialization.Attributes;

namespace InventoryApi.Models;

[BsonDiscriminator("Impressora")]
public class Impressora : InventoryItemBase
{
    public Impressora() => Categoria = "Impressora";

    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Ip { get; set; }
    public string? NumeroSerie { get; set; }
    public string? Ramal { get; set; }

    /// <summary>Ex: "Funcional", "Danificada", "Em estoque".</summary>
    public string? Status { get; set; }

    public string? Observacao { get; set; }
}
