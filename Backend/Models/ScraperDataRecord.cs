using LiteDB;

namespace FaschimPivotApp.Backend.Models;

public class ScraperDataRecord
{
    [BsonId]
    public int Id { get; set; }
    
    public int SearchId { get; set; }
    
    public string? HasConvenzioneFondo { get; set; }
    
    public string? RawContent { get; set; }
    
    public string? ExtractionDate { get; set; }
}
