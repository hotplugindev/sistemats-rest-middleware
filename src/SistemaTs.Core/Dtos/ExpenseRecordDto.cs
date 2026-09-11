using System.ComponentModel.DataAnnotations;

namespace SistemaTs.Core.Dtos;

public sealed class OwnerDto
{
    [RegularExpression(@"^[A-Z0-9]{3}$")]
    public string? CodiceRegione { get; init; }

    [RegularExpression(@"^[A-Z0-9]{3}$")]
    public string? CodiceAsl { get; init; }

    [RegularExpression(@"^[A-Z0-9]{5,6}$")]
    public string? CodiceSsa { get; init; }

    [MaxLength(256)]
    public string? CfProprietario { get; init; }
}

public sealed class ExpenseItemDto
{
    [Required]
    [RegularExpression(@"^(TK|FC|FV|AS|AD|SR|CT|PI|IC|AA|SV|SP)$")]
    public string? TipoSpesa { get; init; }

    [RegularExpression(@"^[12]$")]
    public string? FlagTipoSpesa { get; init; }

    [Range(0.01, 99999.99)]
    public decimal Importo { get; init; }

    [Range(0.00, 100.00)]
    public decimal? AliquotaIva { get; init; }

    [StringLength(10, MinimumLength = 2)]
    public string? NaturaIva { get; init; }
}

public sealed class ExpenseRecordDto
{
    [Required]
    [RegularExpression(@"^\d{11}$")]
    public string? PIva { get; init; }

    public DateOnly DataEmissione { get; init; }

    [Range(1, 999)]
    public int Dispositivo { get; init; } = 1;

    [Required]
    [RegularExpression(@"^[A-Za-z0-9_./\\\-]{1,20}$")]
    public string? NumDocumento { get; init; }

    public DateOnly DataPagamento { get; init; }

    [Range(1, 1)]
    public int? FlagPagamentoAnticipato { get; init; }

    [Required]
    [RegularExpression(@"^[IVRC]$")]
    public string? FlagOperazione { get; init; }

    public string? CfCittadino { get; init; }

    [RegularExpression(@"^(SI|NO)$")]
    public string? PagamentoTracciato { get; init; }

    [RegularExpression(@"^[FD]$")]
    public string? TipoDocumento { get; init; }

    [RegularExpression(@"^[01]$")]
    public string? FlagOpposizione { get; init; }

    [Required]
    [MinLength(1)]
    public List<ExpenseItemDto>? Items { get; init; }
}
