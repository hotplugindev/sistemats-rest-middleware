using Microsoft.AspNetCore.Mvc;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Api.Controllers;

[ApiController]
[Route("api/v1/sistema-ts")]
public sealed class RicevuteController : ControllerBase
{
    private readonly IRicevuteClient _client;

    public RicevuteController(IRicevuteClient client)
    {
        _client = client;
    }

    [HttpPost("esito-invio")]
    [ProducesResponseType(typeof(EsitoInvioResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EsitoInvioResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> EsitoInvii([FromBody] EsitoInvioRequest request, CancellationToken ct)
    {
        var result = await _client.EsitoInviiAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    [HttpPost("dettaglio-errori")]
    [ProducesResponseType(typeof(DettaglioErroriResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DettaglioErroriResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DettaglioErrori([FromBody] DettaglioErroriRequest request, CancellationToken ct)
    {
        var result = await _client.DettaglioErroriAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    [HttpPost("ricevuta-pdf")]
    [ProducesResponseType(typeof(RicevutaPdfResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RicevutaPdfResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> RicevutaPdf([FromBody] RicevutaPdfRequest request, CancellationToken ct)
    {
        var result = await _client.RicevutaPdfAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }
}
