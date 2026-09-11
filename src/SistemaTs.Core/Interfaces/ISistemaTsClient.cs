using SistemaTs.Core.Dtos;

namespace SistemaTs.Core.Interfaces;

public interface ISistemaTsClient
{
    Task<SubmissionResultDto> InviaFileAsync(SoapSubmissionRequest payload, CancellationToken cancellationToken = default);
}
