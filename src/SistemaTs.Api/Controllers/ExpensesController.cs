using Microsoft.AspNetCore.Mvc;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Api.Controllers;

[ApiController]
[Route("api/v1/sistema-ts")]
public sealed class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpPost("submit")]
    [ProducesResponseType(typeof(SubmissionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SubmissionResultDto), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(SubmissionResultDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Submit(
        [FromBody] ExpenseSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.SubmitAsync(request, cancellationToken);

        if (result.Success)
            return Ok(result);

        if (result.Errors.Any(e => e.Contains("Line ") || e.Contains("XML")))
            return UnprocessableEntity(result);

        return StatusCode(StatusCodes.Status502BadGateway, result);
    }
}
