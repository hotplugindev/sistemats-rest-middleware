using Microsoft.AspNetCore.Mvc;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Api.Controllers;

[ApiController]
[Route("api/v1/sistema-ts")]
public sealed class InterrogazioniController : ControllerBase
{
    private readonly IInterrogazioniClient _client;

    public InterrogazioniController(IInterrogazioniClient client)
    {
        _client = client;
    }

    [HttpPost("interrogazione-puntuale")]
    [ProducesResponseType(typeof(InterrogazionePuntualeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InterrogazionePuntualeResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> InterrogazionePuntuale([FromBody] InterrogazionePuntualeRequest request, CancellationToken ct)
    {
        var result = await _client.InterrogazionePuntualeAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    [HttpPost("report-mensile")]
    [ProducesResponseType(typeof(ReportMensileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReportMensileResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReportMensile([FromBody] ReportMensileRequest request, CancellationToken ct)
    {
        var result = await _client.ReportMensileAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }
}
