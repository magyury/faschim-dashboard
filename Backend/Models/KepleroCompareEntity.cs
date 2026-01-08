using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaschimPivotApp.Backend.Models;

[Table("keplero_compare")]
public class KepleroCompareEntity
{
    // [Key]
    // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    // public int Id { get; set; }    
    public int? ItemId { get; set; }
        
    [Key]
    [Required]
    [StringLength(250)]
    public string? Protocollo { get; set; }
    
    [StringLength(100)]
    public string? Coda { get; set; }
    
    [StringLength(250)]
    public string? StatoPratica { get; set; }
    
    [StringLength(250)]
    public string? Stato { get; set; }
    
    [StringLength(250)]
    public string? Esito { get; set; }
    
    public DateTime? DataEsito { get; set; }
    
    [Required]
    [StringLength(100)]
    public string StatoPratica_Keplero { get; set; } = string.Empty;
    
    public int? Riga { get; set; }
}
