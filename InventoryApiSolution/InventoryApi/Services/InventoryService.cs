using System.Text.RegularExpressions;
using InventoryApi.Data;
using InventoryApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InventoryApi.Services;

public class InventoryService : IInventoryService
{
    private readonly MongoDbContext _context;
    private readonly IActivityLogService _activityLog;

    public InventoryService(MongoDbContext context, IActivityLogService activityLog)
    {
        _context = context;
        _activityLog = activityLog;
    }

    public async Task<List<InventoryItemBase>> GetAllAsync(string? categoria = null, string? andar = null, string? local = null, string? unidade = null)
    {
        var filterBuilder = Builders<InventoryItemBase>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrWhiteSpace(categoria))
            filter &= filterBuilder.Eq(x => x.Categoria, categoria);

        if (!string.IsNullOrWhiteSpace(andar))
            filter &= filterBuilder.Eq(x => x.Andar, andar);

        if (!string.IsNullOrWhiteSpace(local))
            filter &= filterBuilder.Regex(x => x.Local, new BsonRegularExpression(local, "i"));

        if (!string.IsNullOrWhiteSpace(unidade))
            filter &= filterBuilder.Regex(x => x.Unidade, new BsonRegularExpression(unidade, "i"));

        return await _context.InventoryItems.Find(filter).ToListAsync();
    }

    public async Task<InventoryItemBase?> GetByIdAsync(string id)
    {
        return await _context.InventoryItems.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<InventoryItemBase> CreateAsync(InventoryItemBase item)
    {
        item.DataImportacao = DateTime.UtcNow;
        await _context.InventoryItems.InsertOneAsync(item);

        await _activityLog.LogAsync("Criar", item.Categoria, item.Unidade, item.Id,
            DescreverItem(item) + " criado manualmente.");

        return item;
    }

    public async Task<List<InventoryItemBase>> CreateManyAsync(IEnumerable<InventoryItemBase> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return list;

        foreach (var item in list)
            item.DataImportacao = DateTime.UtcNow;

        await _context.InventoryItems.InsertManyAsync(list);
        return list;
    }

    public async Task<bool> UpdateAsync(string id, InventoryItemBase item)
    {
        item.Id = id;
        item.DataAtualizacao = DateTime.UtcNow;
        var result = await _context.InventoryItems.ReplaceOneAsync(x => x.Id == id, item);

        if (result.ModifiedCount > 0)
        {
            await _activityLog.LogAsync("Atualizar", item.Categoria, item.Unidade, id,
                DescreverItem(item) + " atualizado.");
        }

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        // Busca o item ANTES de excluir só pra conseguir descrever o que foi excluído no log.
        var existente = await GetByIdAsync(id);

        var result = await _context.InventoryItems.DeleteOneAsync(x => x.Id == id);

        if (result.DeletedCount > 0 && existente is not null)
        {
            await _activityLog.LogAsync("Excluir", existente.Categoria, existente.Unidade, id,
                DescreverItem(existente) + " excluído.");
        }

        return result.DeletedCount > 0;
    }

    public async Task<long> DeleteByUnidadeAsync(string unidade)
    {
        // igualdade exata (não "contains"), mas ignorando maiúscula/minúscula —
        // pra não deletar unidades diferentes que só compartilham parte do nome.
        var pattern = "^" + Regex.Escape(unidade) + "$";
        var filter = Builders<InventoryItemBase>.Filter.Regex(x => x.Unidade, new BsonRegularExpression(pattern, "i"));
        var result = await _context.InventoryItems.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    private static string DescreverItem(InventoryItemBase item)
    {
        return item switch
        {
            Computador c => $"Computador {c.Hostname ?? c.Modelo ?? "sem nome"}",
            Impressora p => $"Impressora {p.Ip ?? p.Modelo ?? "sem nome"}",
            ImpressoraTermica t => $"Etiquetadora {t.Ip ?? t.Modelo ?? "sem nome"}",
            MaterialEstoque m => $"Material '{m.Item ?? "sem nome"}'",
            _ => $"Item {item.Categoria}"
        };
    }
}
