using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventoryApi.Models;

/// <summary>
/// Classe base para todo item de inventário. O MongoDB grava um campo
/// discriminador "_t" automaticamente por causa do [BsonKnownTypes],
/// permitindo guardar Computador, Impressora, ImpressoraTermica e
/// MaterialEstoque na MESMA coleção, mas ainda tipados no C#.
///
/// O [JsonPolymorphic]/[JsonDerivedType] abaixo faz o mesmo papel para a
/// API: quando alguém faz POST /api/inventory com {"categoria":"Impressora", ...},
/// o System.Text.Json já desserializa direto como Impressora.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "categoria", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(Computador), "Computador")]
[JsonDerivedType(typeof(Impressora), "Impressora")]
[JsonDerivedType(typeof(ImpressoraTermica), "ImpressoraTermica")]
[JsonDerivedType(typeof(MaterialEstoque), "MaterialEstoque")]
[BsonKnownTypes(typeof(Computador), typeof(Impressora), typeof(ImpressoraTermica), typeof(MaterialEstoque))]
public abstract class InventoryItemBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Categoria do item: Computador, Notebook, Impressora, ImpressoraTermica, MaterialEstoque...</summary>
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Nome da unidade a que este item pertence (ex: "Roma", "Alemanha") — normalmente extraído do nome do arquivo importado.</summary>
    public string? Unidade { get; set; }

    /// <summary>Andar onde o item está localizado (ex: "17º", "-2", "Terreo").</summary>
    public string? Andar { get; set; }

    /// <summary>Local/setor específico (ex: "Nutrição-ADM", "Posto de Enfermagem").</summary>
    public string? Local { get; set; }

    /// <summary>Nome do arquivo/planilha de origem, para rastreabilidade.</summary>
    public string? Origem { get; set; }

    /// <summary>Nome da aba (sheet) de onde o item veio.</summary>
    public string? PlanilhaOrigem { get; set; }

    public DateTime DataImportacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    /// <summary>
    /// Campos que não têm coluna fixa no modelo mas existem na planilha
    /// (ex: colunas extras que o cliente adicionar no futuro).
    /// Evita perder dado quando a planilha muda.
    /// </summary>
    public Dictionary<string, string?> CamposExtras { get; set; } = new();
}
