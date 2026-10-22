using InventoryApi.Models;
using InventoryApi.Services;

namespace InventoryApi.Tests.Fakes;

public class FakeActivityLogService : IActivityLogService
{
    public List<string> Acoes { get; } = new();

    public Task LogAsync(string acao, string? categoria, string? unidade, string? itemId, string descricao)
    {
        Acoes.Add(acao);
        return Task.CompletedTask;
    }

    public Task<List<ActivityLog>> GetAllAsync(string? unidade = null, string? acao = null, int limit = 200)
        => Task.FromResult(new List<ActivityLog>());
}
