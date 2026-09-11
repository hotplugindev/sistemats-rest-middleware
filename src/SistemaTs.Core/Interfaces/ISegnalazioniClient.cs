using SistemaTs.Core.Dtos;

namespace SistemaTs.Core.Interfaces;

public interface ISegnalazioniClient
{
    Task<DettaglioSegnalazioneResponseDto> DettaglioSegnalazioneAsync(DettaglioSegnalazioneRequest request, CancellationToken ct = default);
    Task<ReportSegnalazioniResponseDto> ReportSegnalazioniAsync(ReportSegnalazioniRequest request, CancellationToken ct = default);
}
