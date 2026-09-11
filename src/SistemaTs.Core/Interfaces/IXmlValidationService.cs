using SistemaTs.Core.Dtos;

namespace SistemaTs.Core.Interfaces;

public interface IXmlValidationService
{
    XmlValidationResult Validate(string xml);
}
