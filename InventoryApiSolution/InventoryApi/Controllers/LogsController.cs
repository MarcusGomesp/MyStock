using InventoryApi.Models;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

/// <summary>Histórico de ações (import, criar, editar, excluir) para acompanhamento/auditoria.</summary>
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly IActivityLogService _activityLog;

    public LogsController(IActivityLogService activityLog)
    {
        _activityLog = activityLog;
    }

    /// <summary>Lista as ações mais recentes, mais nova primeiro. Filtra opcionalmente por unidade e/ou tipo de ação.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ActivityLog>>> GetAll(
        [FromQuery] string? unidade,
        [FromQuery] string? acao,
        [FromQuery] int limit = 200)
    {
        var logs = await _activityLog.GetAllAsync(unidade, acao, limit);
        return Ok(logs);
    }
}
