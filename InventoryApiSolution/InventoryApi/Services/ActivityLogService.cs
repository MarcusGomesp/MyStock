using InventoryApi.Data;
using InventoryApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InventoryApi.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly MongoDbContext _context;

    public ActivityLogService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string acao, string? categoria, string? unidade, string? itemId, string descricao)
    {
        var log = new ActivityLog
        {
            Acao = acao,
            Categoria = categoria,
            Unidade = unidade,
            ItemId = itemId,
            Descricao = descricao,
            Timestamp = DateTime.UtcNow
        };

        // Log nunca deve derrubar a operação principal (import/CRUD) se falhar por algum motivo.
        try { await _context.ActivityLogs.InsertOneAsync(log); }
        catch { /* intencional: falha ao logar não pode quebrar a operação real */ }
    }

    public async Task<List<ActivityLog>> GetAllAsync(string? unidade = null, string? acao = null, int limit = 200)
    {
        var filterBuilder = Builders<ActivityLog>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrWhiteSpace(unidade))
            filter &= filterBuilder.Regex(x => x.Unidade, new BsonRegularExpression(unidade, "i"));

        if (!string.IsNullOrWhiteSpace(acao))
            filter &= filterBuilder.Eq(x => x.Acao, acao);

        return await _context.ActivityLogs.Find(filter)
            .SortByDescending(x => x.Timestamp)
            .Limit(Math.Clamp(limit, 1, 1000))
            .ToListAsync();
    }
}
