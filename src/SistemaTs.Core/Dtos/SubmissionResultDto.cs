namespace SistemaTs.Core.Dtos;

public sealed class SubmissionResultDto
{
    public bool Success { get; init; }

    public string? Protocollo { get; init; }

    public string? CodiceEsito { get; init; }

    public string? DescrizioneEsito { get; init; }

    public string? DataAccoglienza { get; init; }

    public string? NomeFileAllegato { get; init; }

    public string? DimensioneFileAllegato { get; init; }

    public string? IdErrore { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static SubmissionResultDto Failure(params string[] errors) => new()
    {
        Success = false,
        Errors = errors
    };

    public static SubmissionResultDto Failure(IEnumerable<string> errors) => new()
    {
        Success = false,
        Errors = errors.ToList()
    };
}
