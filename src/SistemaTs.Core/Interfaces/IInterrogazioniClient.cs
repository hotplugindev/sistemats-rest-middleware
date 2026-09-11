using SistemaTs.Core.Dtos;

namespace SistemaTs.Core.Interfaces;

public interface IInterrogazioniClient
{
    Task<InterrogazionePuntualeResponseDto> InterrogazionePuntualeAsync(InterrogazionePuntualeRequest request, CancellationToken ct = default);
    Task<ReportMensileResponseDto> ReportMensileAsync(ReportMensileRequest request, CancellationToken ct = default);
}
