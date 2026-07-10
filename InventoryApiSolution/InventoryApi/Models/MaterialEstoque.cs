using MongoDB.Bson.Serialization.Attributes;

namespace InventoryApi.Models;

[BsonDiscriminator("MaterialEstoque")]
public class MaterialEstoque : InventoryItemBase
{
    public MaterialEstoque() => Categoria = "MaterialEstoque";

    public string? Item { get; set; }
    public int? Quantidade { get; set; }
    public string? Status { get; set; }
    public string? Marca { get; set; }
}
