using LiteDB;

namespace FaschimPivotApp.Backend.Models;

public class ScraperSearch
{
    [BsonId]
    public int Id { get; set; }
    
    public string? SearchName { get; set; }
    
    public string? FilterUsed { get; set; }
    
    public DateTime? FetchDate { get; set; }
    
    public bool? Success { get; set; }
    
    public int? CodErrore { get; set; }
    
    public string? Message { get; set; }
    
    public string? DebugMessage { get; set; }
    
    public int? TotalRecords { get; set; }
    
    public int? RecordsFetched { get; set; }
}
