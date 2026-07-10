namespace InventoryApi.Data;

/// <summary>
/// Associa o NOME DA ABA da planilha à categoria padrão dos itens dela.
/// Configurável em appsettings.json (chave "SheetMappings"), sem precisar
/// recompilar a API quando o cliente renomear ou criar uma aba nova.
///
/// Categorias aceitas: "Computador", "Notebook", "Impressora",
/// "ImpressoraTermica", "MaterialEstoque".
///
/// Se uma linha tiver uma coluna chamada "Categoria" preenchida, ela
/// SEMPRE tem prioridade sobre o mapeamento da aba — isso resolve o caso
/// de abas "mistas" (ex: aba "Backup" com impressora, computador e
/// monitor juntos).
/// </summary>
public class SheetMappingSettings
{
    public Dictionary<string, string> Mappings { get; set; } = new();
}
