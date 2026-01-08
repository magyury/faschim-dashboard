using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaschimPivotApp.Backend.Models;

[Table("scraper_data")]
public class ScraperEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [StringLength(500)]
    public string? Url { get; set; }
    
    [StringLength(250)]
    public string? DataSource { get; set; }
    
    public DateTime? FetchDate { get; set; }
    
    [StringLength(100)]
    public string? Status { get; set; }
    
    public string? Content { get; set; }
    
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    
    public DateTime? CreatedAt { get; set; }
}
