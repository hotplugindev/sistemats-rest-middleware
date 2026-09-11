using Microsoft.AspNetCore.Mvc;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Api.Controllers;

[ApiController]
[Route("api/v1/sistema-ts/segnalazioni")]
public sealed class SegnalazioniController : ControllerBase
{
    private readonly ISegnalazioniClient _client;

    public SegnalazioniController(ISegnalazioniClient client)
    {
        _client = client;
    }

    [HttpPost("dettaglio")]
    [ProducesResponseType(typeof(DettaglioSegnalazioneResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DettaglioSegnalazioneResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DettaglioSegnalazione([FromBody] DettaglioSegnalazioneRequest request, CancellationToken ct)
    {
        var result = await _client.DettaglioSegnalazioneAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    [HttpPost("report")]
    [ProducesResponseType(typeof(ReportSegnalazioniResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReportSegnalazioniResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReportSegnalazioni([FromBody] ReportSegnalazioniRequest request, CancellationToken ct)
    {
        var result = await _client.ReportSegnalazioniAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }
}
