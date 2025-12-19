namespace FaschimPivotApp.Backend.Models;

public class PivotRequest
{
    public int StartRow { get; set; }
    public int EndRow { get; set; }
    public List<SortModel>? SortModel { get; set; }
    public Dictionary<string, FilterModel>? FilterModel { get; set; }
}

public class SortModel
{
    public string ColId { get; set; } = string.Empty;
    public string Sort { get; set; } = string.Empty; // "asc" or "desc"
}

public class FilterModel
{
    public string FilterType { get; set; } = string.Empty;
    public string? Filter { get; set; }
    public string? Type { get; set; }
}

public class ProtocolloGrouped
{
    public string NumeroProtocollo { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? UtenteLiquidatore { get; set; }
    public string? DataPresentazione { get; set; }
    public string? StatoPratica { get; set; }
    public string? FormaAssistenza { get; set; }
}
