using SistemaTs.Core.Dtos;

namespace SistemaTs.Core.Interfaces;

public interface IDocumentoSpesaClient
{
    Task<SincronoResultDto> InserimentoAsync(DocumentoSpesaSyncRequest request, CancellationToken ct = default);
    Task<SincronoResultDto> VariazioneAsync(DocumentoSpesaSyncRequest request, CancellationToken ct = default);
    Task<SincronoResultDto> RimborsoAsync(RimborsoSincronoRequestDto request, CancellationToken ct = default);
    Task<SincronoResultDto> CancellazioneAsync(CancellazioneSincronoRequestDto request, CancellationToken ct = default);
}
