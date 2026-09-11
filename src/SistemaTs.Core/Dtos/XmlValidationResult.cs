namespace SistemaTs.Core.Dtos;

public sealed record XmlValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static XmlValidationResult Valid() => new(true, Array.Empty<string>());
}
