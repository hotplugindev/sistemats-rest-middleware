namespace SistemaTs.Core.Dtos;

public sealed class DocumentoSpesaSyncRequest
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public OwnerDto? Owner { get; init; }
    public required ExpenseRecordDto Expense { get; init; }
}

public sealed class RimborsoSincronoRequestDto
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public OwnerDto? Owner { get; init; }
    public required string PIvaOriginale { get; init; }
    public required DateOnly DataEmissioneOriginale { get; init; }
    public int DispositivoOriginale { get; init; } = 1;
    public required string NumDocumentoOriginale { get; init; }
    public required ExpenseRecordDto Expense { get; init; }
}

public sealed class CancellazioneSincronoRequestDto
{
    public required ProviderCredentialsDto Credentials { get; init; }
    public OwnerDto? Owner { get; init; }
    public required string PIva { get; init; }
    public required DateOnly DataEmissione { get; init; }
    public int Dispositivo { get; init; } = 1;
    public required string NumDocumento { get; init; }
}

public sealed class SincronoResultDto
{
    public bool Success { get; init; }
    public string? EsitoChiamata { get; init; }
    public string? Protocollo { get; init; }
    public IReadOnlyList<MessaggioDto> Messaggi { get; init; } = Array.Empty<MessaggioDto>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
