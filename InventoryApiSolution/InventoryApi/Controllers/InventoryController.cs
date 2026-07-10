using InventoryApi.DTOs;
using InventoryApi.Models;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ExcelImportService _importService;
    private readonly CsvImportService _csvImportService;
    private readonly ExcelExportService _exportService;
    private readonly CsvExportService _csvExportService;

    public InventoryController(
        IInventoryService inventoryService,
        ExcelImportService importService,
        CsvImportService csvImportService,
        ExcelExportService exportService,
        CsvExportService csvExportService)
    {
        _inventoryService = inventoryService;
        _importService = importService;
        _csvImportService = csvImportService;
        _exportService = exportService;
        _csvExportService = csvExportService;
    }

    /// <summary>Lista itens do inventário, com filtros opcionais (inclui "unidade").</summary>
    [HttpGet]
    public async Task<ActionResult<List<InventoryItemBase>>> GetAll(
        [FromQuery] string? categoria,
        [FromQuery] string? andar,
        [FromQuery] string? local,
        [FromQuery] string? unidade)
    {
        var items = await _inventoryService.GetAllAsync(categoria, andar, local, unidade);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryItemBase>> GetById(string id)
    {
        var item = await _inventoryService.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Cria um item manualmente via JSON. O corpo deve conter "categoria"
    /// (Computador, Impressora, ImpressoraTermica ou MaterialEstoque) mais
    /// os campos daquele tipo. Campos não obrigatórios podem vir nulos ou ausentes.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InventoryItemBase>> Create([FromBody] InventoryItemBase item)
    {
        var created = await _inventoryService.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] InventoryItemBase item)
    {
        var updated = await _inventoryService.UpdateAsync(id, item);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _inventoryService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Importa um arquivo .xlsx ou .csv. Cada arquivo pertence a UMA unidade
    /// (ex: "Unidade Roma.csv" -> unidade "Roma"), detectada automaticamente
    /// pelo nome do arquivo ou informada em ?unidade=. Reimportar um arquivo
    /// da MESMA unidade SUBSTITUI os itens dessa unidade pelo conteúdo atual
    /// do arquivo (sincroniza — não duplica). Se o arquivo vier vazio ou sem
    /// nenhum item reconhecido, nada é alterado (proteção contra apagar tudo
    /// sem querer).
    ///
    /// - .xlsx: cada aba é lida e convertida em itens tipados (categoria por aba, configurável em appsettings).
    /// - .csv: é uma tabela só. A categoria vem da coluna "Categoria" na linha,
    ///   ou do parâmetro de query "categoria" (categoria padrão pra esse arquivo inteiro).
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Import(IFormFile file, [FromQuery] string? categoria, [FromQuery] string? unidade)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Nenhum arquivo enviado.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        using var stream = file.OpenReadStream();

        ImportResultDto result;
        switch (extension)
        {
            case ".xlsx":
                result = await _importService.ImportAsync(stream, file.FileName, unidade);
                break;
            case ".csv":
                result = await _csvImportService.ImportAsync(stream, file.FileName, categoria, unidade);
                break;
            default:
                return BadRequest("Apenas arquivos .xlsx ou .csv são suportados.");
        }

        return Ok(result);
    }

    /// <summary>
    /// Exporta o inventário (com filtros opcionais, incluindo "unidade") de
    /// volta para arquivo. Use ?formato=csv para gerar um .csv pronto para
    /// abrir no Excel (delimitador ';' e BOM UTF-8), ou omita/formato=xlsx
    /// para gerar um .xlsx com uma aba por categoria.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? categoria,
        [FromQuery] string? andar,
        [FromQuery] string? local,
        [FromQuery] string? unidade,
        [FromQuery] string formato = "xlsx")
    {
        var items = await _inventoryService.GetAllAsync(categoria, andar, local, unidade);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmm");

        if (formato.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = _csvExportService.Export(items);
            return File(csvBytes, "text/csv", $"inventario_{timestamp}.csv");
        }

        var bytes = _exportService.Export(items);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"inventario_{timestamp}.xlsx");
    }
}
