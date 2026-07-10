namespace InventoryApi.Services;

/// <summary>
/// Lista, num só lugar, todos os nomes/apelidos de coluna que o importador
/// reconhece por campo. Usado por InventoryItemBuilder (pra ler o valor) e
/// também pelos serviços de import (pra montar o diagnóstico "quais colunas
/// foram detectadas no seu arquivo", que ajuda a achar rápido um problema
/// tipo "coluna 'IP' não foi reconhecida porque no arquivo ela se chama
/// 'Endereco IP'".
/// </summary>
public static class FieldAliasCatalog
{
    public static readonly Dictionary<string, string[]> Aliases = new()
    {
        ["Categoria"] = new[] { "Categoria" },
        ["Unidade"] = new[] { "Unidade" },
        ["Andar"] = new[] { "Andar" },
        ["Local"] = new[] { "Local" },
        ["Host"] = new[] { "Host", "Hostname" },
        ["Marca"] = new[] { "Marca" },
        ["Modelo"] = new[] { "Modelo" },
        ["Patrimonio"] = new[] { "Patrimonio", "Patrimônio" },
        ["Serial number"] = new[] { "Serial number", "Serial Number", "Numero de serie", "Nº de Serie" },
        ["SSD/HD"] = new[] { "SSD/HD", "SSD", "HD" },
        ["SO"] = new[] { "SO", "Sistema Operacional" },
        ["Memoria RAM"] = new[] { "Memoria RAM", "Memória RAM", "RAM" },
        ["Processador"] = new[] { "Processador", "CPU" },
        ["Monitores SN"] = new[] { "Monitores SN", "Monitor SN", "Monitores  SN" },
        ["Monitor patrimonio"] = new[] { "Monitor patrimonio", "Monitor Patrimônio" },
        ["Monitor modelo"] = new[] { "Monitor modelo", "Monitor Modelo" },
        ["IP"] = new[] { "IP" },
        ["Nº de Serie (Impressora)"] = new[] { "Nº de Serie", "N de Serie", "Numero de Serie", "Serial number" },
        ["Ramal"] = new[] { "Ramal" },
        ["Status"] = new[] { "Status" },
        ["Uso"] = new[] { "Uso" },
        ["Item"] = new[] { "Item" },
        ["Quantidade"] = new[] { "Quantidade" },
    };
}
