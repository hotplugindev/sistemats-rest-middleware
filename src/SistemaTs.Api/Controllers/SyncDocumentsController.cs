using Microsoft.AspNetCore.Mvc;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Api.Controllers;

[ApiController]
[Route("api/v1/sistema-ts/sync")]
public sealed class SyncDocumentsController : ControllerBase
{
    private readonly IDocumentoSpesaClient _documentoSpesaClient;

    public SyncDocumentsController(IDocumentoSpesaClient documentoSpesaClient)
    {
        _documentoSpesaClient = documentoSpesaClient;
    }

    [HttpPost("inserimento")]
    [ProducesResponseType(typeof(SincronoResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SincronoResultDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Inserimento([FromBody] DocumentoSpesaSyncRequest request, CancellationToken ct)
    {
        var result = await _documentoSpesaClient.InserimentoAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    [HttpPost("variazione")]
    [ProducesResponseType(typeof(SincronoResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SincronoResultDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Variazione([FromBody] DocumentoSpesaSyncRequest request, CancellationToken ct)
    {
        var result = await _documentoSpesaClient.VariazioneAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    [HttpPost("rimborso")]
    [ProducesResponseType(typeof(SincronoResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SincronoResultDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Rimborso([FromBody] RimborsoSincronoRequestDto request, CancellationToken ct)
    {
        var result = await _documentoSpesaClient.RimborsoAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    [HttpPost("cancellazione")]
    [ProducesResponseType(typeof(SincronoResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SincronoResultDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Cancellazione([FromBody] CancellazioneSincronoRequestDto request, CancellationToken ct)
    {
        var result = await _documentoSpesaClient.CancellazioneAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }
}
