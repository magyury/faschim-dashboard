using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaschimPivotApp.Backend.Models;

[Table("FullKeplero")]
public class FullKeplero
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string NumeroProtocollo { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? UtenteLiquidatore { get; set; }

    [Required]
    [MaxLength(50)]
    public string DataPresentazione { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DataInserimento { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Modified { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? DescrizioneGruppoTariffa { get; set; }

    [Required]
    [MaxLength(50)]
    public string FormaAssistenza { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ImportoRichiesto { get; set; }

    [MaxLength(50)]
    public string? ImportoRiconosciuto { get; set; }

    [MaxLength(50)]
    public string? DataPagamento { get; set; }

    [Required]
    [MaxLength(50)]
    public string CognomePersona { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NomePersona { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CognomeBeneficiario { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NomeBeneficiario { get; set; } = string.Empty;

    [MaxLength(1)]
    public string? UnisalInviato { get; set; }

    [Required]
    [MaxLength(50)]
    public string StatoPratica { get; set; } = string.Empty;
}
