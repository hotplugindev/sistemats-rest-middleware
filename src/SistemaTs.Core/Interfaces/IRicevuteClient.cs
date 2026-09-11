using SistemaTs.Core.Dtos;

namespace SistemaTs.Core.Interfaces;

public interface IRicevuteClient
{
    Task<EsitoInvioResponseDto> EsitoInviiAsync(EsitoInvioRequest request, CancellationToken ct = default);
    Task<DettaglioErroriResponseDto> DettaglioErroriAsync(DettaglioErroriRequest request, CancellationToken ct = default);
    Task<RicevutaPdfResponseDto> RicevutaPdfAsync(RicevutaPdfRequest request, CancellationToken ct = default);
}
