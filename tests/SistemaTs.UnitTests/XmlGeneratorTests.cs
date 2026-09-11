using System.Xml.Linq;
using SistemaTs.Core.Dtos;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class XmlGeneratorTests
{
    private readonly XmlGeneratorService _sut = new();

    [Fact]
    public void GenerateXml_ProducesValidStructure()
    {
        var request = new ExpenseSubmissionRequest
        {
            Credentials = new ProviderCredentialsDto
            {
                Username = "PROVAX00X00X000Y",
                Password = "Salve123",
                Pincode = "1234567890"
            },
            Owner = new OwnerDto { CfProprietario = "ENCRYPTED_CF" },
            Expenses = new List<ExpenseRecordDto>
            {
                new()
                {
                    PIva = "00265910661",
                    DataEmissione = new DateOnly(2024, 1, 15),
                    Dispositivo = 1,
                    NumDocumento = "1234567",
                    DataPagamento = new DateOnly(2024, 1, 15),
                    FlagOperazione = "I",
                    CfCittadino = "ENCRYPTED_CF_CITTADINO",
                    PagamentoTracciato = "NO",
                    TipoDocumento = "F",
                    FlagOpposizione = "0",
                    Items = new List<ExpenseItemDto>
                    {
                        new() { TipoSpesa = "SR", Importo = 52.01m, AliquotaIva = 10.00m }
                    }
                }
            }
        };

        var xml = _sut.GenerateXml(request);
        var doc = XDocument.Parse(xml);

        Assert.Equal("precompilata", doc.Root!.Name.LocalName);
        Assert.NotNull(doc.Root.Element("proprietario"));
        Assert.Equal("ENCRYPTED_CF", doc.Root.Element("proprietario")!.Element("cfProprietario")!.Value);

        var docSpesa = doc.Root.Element("documentoSpesa");
        Assert.NotNull(docSpesa);
        Assert.Equal("00265910661", docSpesa!.Element("idSpesa")!.Element("pIva")!.Value);
        Assert.Equal("2024-01-15", docSpesa.Element("idSpesa")!.Element("dataEmissione")!.Value);
        Assert.Equal("1", docSpesa.Element("idSpesa")!.Element("numDocumentoFiscale")!.Element("dispositivo")!.Value);
        Assert.Equal("1234567", docSpesa.Element("idSpesa")!.Element("numDocumentoFiscale")!.Element("numDocumento")!.Value);
        Assert.Equal("NO", docSpesa.Element("pagamentoTracciato")!.Value);
        Assert.Equal("F", docSpesa.Element("tipoDocumento")!.Value);
        Assert.Equal("0", docSpesa.Element("flagOpposizione")!.Value);

        var voce = docSpesa.Element("voceSpesa");
        Assert.NotNull(voce);
        Assert.Equal("SR", voce!.Element("tipoSpesa")!.Value);
        Assert.Equal("52.01", voce.Element("importo")!.Value);
        Assert.Equal("10.00", voce.Element("aliquotaIVA")!.Value);
    }

    [Fact]
    public void GenerateXml_WithNaturaIva_OmitsAliquotaIva()
    {
        var request = new ExpenseSubmissionRequest
        {
            Credentials = new ProviderCredentialsDto
            {
                Username = "TEST",
                Password = "TEST",
                Pincode = "1234567890"
            },
            Expenses = new List<ExpenseRecordDto>
            {
                new()
                {
                    PIva = "00265910661",
                    DataEmissione = new DateOnly(2024, 3, 1),
                    Dispositivo = 1,
                    NumDocumento = "A001",
                    DataPagamento = new DateOnly(2024, 3, 1),
                    FlagOperazione = "I",
                    Items = new List<ExpenseItemDto>
                    {
                        new() { TipoSpesa = "FC", Importo = 25.50m, NaturaIva = "N1" }
                    }
                }
            }
        };

        var xml = _sut.GenerateXml(request);
        var doc = XDocument.Parse(xml);
        var voce = doc.Root!.Element("documentoSpesa")!.Element("voceSpesa")!;

        Assert.Null(voce.Element("aliquotaIVA"));
        Assert.Equal("N1", voce.Element("naturaIVA")!.Value);
    }

    [Fact]
    public void GenerateXml_MultipleExpenses_ProducesMultipleDocumentoSpesa()
    {
        var request = new ExpenseSubmissionRequest
        {
            Credentials = new ProviderCredentialsDto
            {
                Username = "TEST",
                Password = "TEST",
                Pincode = "1234567890"
            },
            Expenses = new List<ExpenseRecordDto>
            {
                new()
                {
                    PIva = "00265910661",
                    DataEmissione = new DateOnly(2024, 1, 1),
                    Dispositivo = 1,
                    NumDocumento = "001",
                    DataPagamento = new DateOnly(2024, 1, 1),
                    FlagOperazione = "I",
                    Items = new List<ExpenseItemDto> { new() { TipoSpesa = "SR", Importo = 10.00m, AliquotaIva = 22.00m } }
                },
                new()
                {
                    PIva = "00265910661",
                    DataEmissione = new DateOnly(2024, 2, 1),
                    Dispositivo = 1,
                    NumDocumento = "002",
                    DataPagamento = new DateOnly(2024, 2, 1),
                    FlagOperazione = "I",
                    Items = new List<ExpenseItemDto> { new() { TipoSpesa = "FC", Importo = 20.00m, AliquotaIva = 10.00m } }
                }
            }
        };

        var xml = _sut.GenerateXml(request);
        var doc = XDocument.Parse(xml);

        Assert.Equal(2, doc.Root!.Elements("documentoSpesa").Count());
    }
}
