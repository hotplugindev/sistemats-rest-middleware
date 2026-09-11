namespace SistemaTs.Core.Dtos;

public sealed class SoapSubmissionRequest
{
    public required string NomeFileAllegato { get; init; }

    public required string PincodeInvianteCifrato { get; init; }

    public OwnerDto? Owner { get; init; }

    public string? Opzionale1 { get; init; }

    public string? Opzionale2 { get; init; }

    public string? Opzionale3 { get; init; }

    public required byte[] ZipContent { get; init; }

    public required ProviderCredentialsDto Credentials { get; init; }
}
