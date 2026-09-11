namespace SistemaTs.Core.Dtos;

public sealed class InterrogazionePuntualeRequest
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public OwnerDto? Owner { get; init; }
    public required string PIva { get; init; }
    public required DateOnly DataEmissione { get; init; }
    public int Dispositivo { get; init; } = 1;
    public required string NumDocumento { get; init; }
}

public sealed class InterrogazionePuntualeResponseDto
{
    public bool Success { get; init; }
    public string? EsitoChiamata { get; init; }
    public DocumentoFiscaleDto? DocumentoFiscale { get; init; }
    public List<MessaggioDto> ListaMessaggi { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public sealed class DocumentoFiscaleDto
{
    public string? PIva { get; set; }
    public string? DataEmissione { get; set; }
    public int Dispositivo { get; set; }
    public string? NumDocumento { get; set; }
    public string? DataPagamento { get; set; }
    public List<TotaleVoceSpesaDto> TotaliVociSpesa { get; set; } = new();
    public List<TotaleVoceSpesaDto> TotaliVociSpesaRimborsate { get; set; } = new();
    public string? Protocollo { get; set; }
    public string? NomeFile { get; set; }
    public string? DataInvio { get; set; }
    public string? TipoInvio { get; set; }
    public List<ErroreDocumentoDto> ListaErroriDocumento { get; set; } = new();
}

public sealed class TotaleVoceSpesaDto
{
    public string? TipoSpesa { get; init; }
    public double Importo { get; init; }
}

public sealed class ErroreDocumentoDto
{
    public string? Codice { get; init; }
    public string? Descrizione { get; init; }
    public string? Tipo { get; init; }
}

public sealed class MessaggioDto
{
    public string? Codice { get; init; }
    public string? Descrizione { get; init; }
    public string? Tipo { get; init; }
}
