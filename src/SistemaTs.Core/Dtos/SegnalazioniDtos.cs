namespace SistemaTs.Core.Dtos;

public sealed class DettaglioSegnalazioneRequest
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public required OwnerDto Owner { get; init; }
    public required string PIva { get; init; }
    public required DateOnly DataEmissione { get; init; }
    public int Dispositivo { get; init; } = 1;
    public required string NumDocumento { get; init; }
}

public sealed class DettaglioSegnalazioneResponseDto
{
    public bool Success { get; init; }
    public string? EsitoChiamata { get; init; }
    public DocumentoSegnalazioneDto? DocumentoFiscale { get; init; }
    public List<MessaggioDto> ListaMessaggi { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public sealed class DocumentoSegnalazioneDto
{
    public string? PIva { get; set; }
    public string? DataEmissione { get; set; }
    public int Dispositivo { get; set; }
    public string? NumDocumento { get; set; }
    public string? DataPagamento { get; set; }
    public string? DataInvio { get; set; }
    public string? Protocollo { get; set; }
    public string? NomeFile { get; set; }
    public List<string> ListaSegnalazioniDocumento { get; set; } = new();
    public List<VoceSpesaSegnalazioneDto> ListaVociSpesa { get; set; } = new();
    public List<VoceSpesaSegnalazioneDto> ListaVociSpesaRimborsate { get; set; } = new();
}

public sealed class VoceSpesaSegnalazioneDto
{
    public string? TipoSpesa { get; init; }
    public double Importo { get; init; }
    public string? SegnalazioneImporto { get; init; }
    public string? SegnalazioneTipoVoceSpesa { get; init; }
}

public sealed class ReportSegnalazioniRequest
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public required OwnerDto Owner { get; init; }
    public required DateOnly DataIniPeriodoSegnalazione { get; init; }
    public required DateOnly DataFinPeriodoSegnalazione { get; init; }
}

public sealed class ReportSegnalazioniResponseDto
{
    public bool Success { get; init; }
    public string? EsitoChiamata { get; init; }
    public byte[]? CsvContent { get; init; }
    public List<MessaggioDto> ListaMessaggi { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
