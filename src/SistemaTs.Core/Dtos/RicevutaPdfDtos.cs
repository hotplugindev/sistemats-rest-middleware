namespace SistemaTs.Core.Dtos;

public sealed class RicevutaPdfRequest
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public required string Protocollo { get; init; }
}

public sealed class RicevutaPdfResponseDto
{
    public bool Success { get; init; }
    public string? EsitoChiamata { get; init; }
    public byte[]? PdfContent { get; init; }
    public List<EsitoNegativoDto> EsitiNegativi { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
