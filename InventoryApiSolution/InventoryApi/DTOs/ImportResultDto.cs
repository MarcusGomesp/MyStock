namespace InventoryApi.DTOs;

public class ImportResultDto
{
    public string Arquivo { get; set; } = string.Empty;

    /// <summary>Nome da unidade detectada (do nome do arquivo, ou informada manualmente).</summary>
    public string? Unidade { get; set; }

    /// <summary>Quantos itens dessa unidade existiam antes e foram removidos por causa desta reimportação (sincronização).</summary>
    public long ItensSubstituidos { get; set; }

    public List<SheetImportResultDto> Planilhas { get; set; } = new();
    public int TotalItensImportados => Planilhas.Sum(p => p.ItensImportados);
}

public class SheetImportResultDto
{
    public string NomePlanilha { get; set; } = string.Empty;
    public string CategoriaPadrao { get; set; } = string.Empty;
    public int ItensImportados { get; set; }
    public int LinhasIgnoradas { get; set; }
    public List<string> Avisos { get; set; } = new();

    /// <summary>
    /// Diagnóstico: para cada campo conhecido, diz se uma coluna com esse
    /// nome (ou algum apelido dela) foi encontrada no cabeçalho do arquivo.
    /// Se um campo que devia vir preenchido aparece "false" aqui, é sinal de
    /// que o nome da coluna no seu arquivo não bate com nenhum apelido
    /// reconhecido — renomeie a coluna no arquivo pra um dos nomes esperados.
    /// </summary>
    public Dictionary<string, bool> ColunasDetectadas { get; set; } = new();
}
