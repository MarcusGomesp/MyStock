using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventoryApi.Models;

/// <summary>Registro de uma ação relevante (import, criar, editar, excluir) pra dar rastreabilidade ao inventário.</summary>
public class ActivityLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Criar, Atualizar, Excluir, ImportarCsv, ImportarExcel.</summary>
    public string Acao { get; set; } = string.Empty;

    public string? Categoria { get; set; }
    public string? Unidade { get; set; }
    public string? ItemId { get; set; }

    /// <summary>Resumo legível do que aconteceu (ex: "Impressora 10.2.93.254 excluída" ou "Roma.csv: 4 importados, 2 substituídos").</summary>
    public string Descricao { get; set; } = string.Empty;
}
