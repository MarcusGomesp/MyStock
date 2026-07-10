using InventoryApi.Models;

namespace InventoryApi.Services;

public interface IInventoryService
{
    Task<List<InventoryItemBase>> GetAllAsync(string? categoria = null, string? andar = null, string? local = null, string? unidade = null);
    Task<InventoryItemBase?> GetByIdAsync(string id);
    Task<InventoryItemBase> CreateAsync(InventoryItemBase item);
    Task<List<InventoryItemBase>> CreateManyAsync(IEnumerable<InventoryItemBase> items);
    Task<bool> UpdateAsync(string id, InventoryItemBase item);
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Remove TODOS os itens de uma unidade específica (comparação exata,
    /// sem diferenciar maiúscula/minúscula). Usado antes de reimportar um
    /// arquivo dessa unidade, pra sincronizar o banco com o arquivo atual
    /// em vez de duplicar itens a cada reimportação.
    /// </summary>
    Task<long> DeleteByUnidadeAsync(string unidade);
}
