using Microsoft.Extensions.Options;
using SistemaTs.Infrastructure.Configuration;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class XmlValidationTests
{
    private static XmlValidationService CreateValidator()
    {
        var xsdPath = Path.Combine(AppContext.BaseDirectory, "730_precompilata.xsd");
        if (!File.Exists(xsdPath))
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "730_precompilata.xsd")))
                dir = dir.Parent;
            if (dir is not null)
                xsdPath = Path.Combine(dir.FullName, "src", "SistemaTs.Infrastructure", "730_precompilata.xsd");
        }

        var options = Options.Create(new SistemaTsOptions { XsdSchemaPath = xsdPath });
        return new XmlValidationService(options);
    }

    [Fact]
    public void Validate_ValidXml_ReturnsValid()
    {
        var sut = CreateValidator();

        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <precompilata xsi:noNamespaceSchemaLocation="730_precompilata.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <proprietario>
                <cfProprietario>TESTCF</cfProprietario>
              </proprietario>
              <documentoSpesa>
                <idSpesa>
                  <pIva>00265910661</pIva>
                  <dataEmissione>2024-01-15</dataEmissione>
                  <numDocumentoFiscale>
                    <dispositivo>1</dispositivo>
                    <numDocumento>1234567</numDocumento>
                  </numDocumentoFiscale>
                </idSpesa>
                <dataPagamento>2024-01-15</dataPagamento>
                <flagOperazione>I</flagOperazione>
                <voceSpesa>
                  <tipoSpesa>SR</tipoSpesa>
                  <importo>52.01</importo>
                </voceSpesa>
              </documentoSpesa>
            </precompilata>
            """;

        var result = sut.Validate(xml);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_InvalidXml_ReturnsErrors()
    {
        var sut = CreateValidator();

        var xml = "<notvalid>content</notvalid>";

        var result = sut.Validate(xml);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_MalformedXml_ReturnsParseError()
    {
        var sut = CreateValidator();

        var xml = "<broken><unclosed>";

        var result = sut.Validate(xml);
        Assert.False(result.IsValid);
    }
}
