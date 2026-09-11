using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Options;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Configuration;

namespace SistemaTs.Infrastructure.Services;

public sealed class XmlValidationService : IXmlValidationService
{
    private readonly XmlSchemaSet _schemaSet;

    public XmlValidationService(IOptions<SistemaTsOptions> options)
    {
        _schemaSet = new XmlSchemaSet();
        var xsdPath = SanitelCryptoService.ResolvePath(options.Value.XsdSchemaPath);
        _schemaSet.Add(null, xsdPath);
        _schemaSet.Compile();
    }

    public XmlValidationResult Validate(string xml)
    {
        var errors = new List<string>();

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = _schemaSet
        };
        settings.ValidationEventHandler += (_, e) =>
        {
            if (e.Severity == XmlSeverityType.Error)
                errors.Add($"Line {e.Exception?.LineNumber}: {e.Message}");
        };

        using var reader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(reader, settings);
        try
        {
            while (xmlReader.Read()) { }
        }
        catch (XmlException ex)
        {
            errors.Add($"XML parse error: {ex.Message}");
        }

        return errors.Count == 0
            ? XmlValidationResult.Valid()
            : new XmlValidationResult(false, errors);
    }
}
