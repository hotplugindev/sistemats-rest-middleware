using SistemaTs.Core.Dtos;

namespace SistemaTs.Core.Interfaces;

public interface IExpenseService
{
    Task<SubmissionResultDto> SubmitAsync(ExpenseSubmissionRequest request, CancellationToken cancellationToken = default);
}
