namespace SistemaTs.Core.Dtos;

public sealed class ReportMensileRequest
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public OwnerDto? Owner { get; init; }
    public required string AnnoMese { get; init; }
    public required string TipoEstrazione { get; init; }
}

public sealed class ReportMensileResponseDto
{
    public bool Success { get; init; }
    public string? EsitoChiamata { get; init; }
    public byte[]? CsvContent { get; init; }
    public List<MessaggioDto> ListaMessaggi { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
