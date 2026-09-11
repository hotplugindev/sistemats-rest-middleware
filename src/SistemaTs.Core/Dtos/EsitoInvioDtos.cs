namespace SistemaTs.Core.Dtos;

public sealed class EsitoInvioRequest
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public string? DataInizio { get; init; }
    public string? DataFine { get; init; }
    public string? Protocollo { get; init; }
}

public sealed class EsitoInvioResponseDto
{
    public bool Success { get; init; }
    public string? EsitoChiamata { get; init; }
    public string? DescrizioneEsito { get; init; }
    public List<EsitoPositivoDto> EsitiPositivi { get; init; } = new();
    public List<EsitoNegativoDto> EsitiNegativi { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public sealed class EsitoPositivoDto
{
    public string? Protocollo { get; init; }
    public string? DataInvio { get; init; }
    public int Stato { get; init; }
    public string? Descrizione { get; init; }
    public long NInviati { get; init; }
    public long NAccolti { get; init; }
    public long NWarnings { get; init; }
    public long NErrori { get; init; }
}

public sealed class EsitoNegativoDto
{
    public string? Codice { get; init; }
    public string? Descrizione { get; init; }
}
