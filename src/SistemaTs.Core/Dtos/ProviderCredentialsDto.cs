using System.ComponentModel.DataAnnotations;

namespace SistemaTs.Core.Dtos;

public sealed class ProviderCredentialsDto
{
    [Required]
    public string? Username { get; init; }

    [Required]
    public string? Password { get; init; }

    [Required]
    public string? Pincode { get; init; }
}
