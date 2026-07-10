using InventoryApi.Models;

namespace InventoryApi.Services;

public interface IActivityLogService
{
    Task LogAsync(string acao, string? categoria, string? unidade, string? itemId, string descricao);
    Task<List<ActivityLog>> GetAllAsync(string? unidade = null, string? acao = null, int limit = 200);
}
