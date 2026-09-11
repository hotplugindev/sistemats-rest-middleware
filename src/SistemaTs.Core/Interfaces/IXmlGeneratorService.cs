using SistemaTs.Core.Dtos;

namespace SistemaTs.Core.Interfaces;

public interface IXmlGeneratorService
{
    string GenerateXml(ExpenseSubmissionRequest request);
}
