using MongoDB.Bson.Serialization.Attributes;

namespace InventoryApi.Models;

/// <summary>Cobre etiquetadoras térmicas (ex: GA-2408T) e impressoras de backup tipo Tally.</summary>
[BsonDiscriminator("ImpressoraTermica")]
public class ImpressoraTermica : InventoryItemBase
{
    public ImpressoraTermica() => Categoria = "ImpressoraTermica";

    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Ip { get; set; }
    public string? NumeroSerie { get; set; }

    /// <summary>Ex: "Etiqueta Laboratorio", "Etiqueta Pacientes".</summary>
    public string? Uso { get; set; }

    public string? Status { get; set; }
}
