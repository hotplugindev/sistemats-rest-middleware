using System.ComponentModel.DataAnnotations;

namespace SistemaTs.Core.Dtos;

public sealed class ExpenseSubmissionRequest
{
    [Required]
    public ProviderCredentialsDto? Credentials { get; init; }

    public OwnerDto? Owner { get; init; }

    [RegularExpression(@"^\p{IsBasicLatin}{6,60}$")]
    public string? AttachmentName { get; init; }

    public string? XmlEntryName { get; init; }

    [Required]
    [MinLength(1)]
    public List<ExpenseRecordDto>? Expenses { get; init; }
}
