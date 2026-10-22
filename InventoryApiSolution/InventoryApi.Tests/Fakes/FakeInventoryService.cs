using InventoryApi.Models;
using InventoryApi.Services;

namespace InventoryApi.Tests.Fakes;

/// <summary>Repositório em memória: guarda o que o serviço de importação mandou gravar.</summary>
public class FakeInventoryService : IInventoryService
{
    public List<InventoryItemBase> Itens { get; } = new();
    public List<string> UnidadesApagadas { get; } = new();
    public int ChamadasCreateMany { get; private set; }

    public Task<List<InventoryItemBase>> GetAllAsync(string? categoria = null, string? andar = null, string? local = null, string? unidade = null)
        => Task.FromResult(Itens.ToList());

    public Task<InventoryItemBase?> GetByIdAsync(string id)
        => Task.FromResult(Itens.FirstOrDefault(i => i.Id == id));

    public Task<InventoryItemBase> CreateAsync(InventoryItemBase item)
    {
        Itens.Add(item);
        return Task.FromResult(item);
    }

    public Task<List<InventoryItemBase>> CreateManyAsync(IEnumerable<InventoryItemBase> items)
    {
        ChamadasCreateMany++;
        var lista = items.ToList();
        Itens.AddRange(lista);
        return Task.FromResult(lista);
    }

    public Task<bool> UpdateAsync(string id, InventoryItemBase item) => Task.FromResult(true);

    public Task<bool> DeleteAsync(string id) => Task.FromResult(Itens.RemoveAll(i => i.Id == id) > 0);

    public Task<long> DeleteByUnidadeAsync(string unidade)
    {
        UnidadesApagadas.Add(unidade);
        long removidos = Itens.RemoveAll(i => string.Equals(i.Unidade, unidade, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(removidos);
    }
}
