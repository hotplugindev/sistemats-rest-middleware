using System.Net;
using System.Text;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class RicevuteSoapClientTests
{
    private static readonly string EsitoInviiSuccessResponse = """
        <?xml version="1.0" encoding="UTF-8"?>
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
          <soapenv:Body>
            <ns2:EsitoInviiResponse xmlns:ns2="http://esitoinvio.p730.sanita.sogei.it/">
              <DatiOutputRichiesta>
                <esitoChiamata>0</esitoChiamata>
                <descrizioneEsito>OK</descrizioneEsito>
                <esitiPositivi>
                  <dettagliEsito>
                    <protocollo>PROTO1</protocollo>
                    <dataInvio>2024-01-15</dataInvio>
                    <stato>3</stato>
                    <descrizione>Accettato</descrizione>
                    <nInviati>10</nInviati>
                    <nAccolti>8</nAccolti>
                    <nWarnings>1</nWarnings>
                    <nErrori>1</nErrori>
                  </dettagliEsito>
                </esitiPositivi>
              </DatiOutputRichiesta>
            </ns2:EsitoInviiResponse>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    private static RicevuteSoapClient CreateClient(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var envSettings = new FakeEnvironmentSettings();
        var crypto = new FakeCryptoService();
        return new RicevuteSoapClient(httpClient, envSettings, crypto);
    }

    [Fact]
    public async Task EsitoInviiAsync_Success_ParsesPositivi()
    {
        var sut = CreateClient(HttpStatusCode.OK, EsitoInviiSuccessResponse);

        var result = await sut.EsitoInviiAsync(new EsitoInvioRequest
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
            DataInizio = "2024-01-01",
            DataFine = "2024-01-31"
        });

        Assert.True(result.Success);
        Assert.Single(result.EsitiPositivi);
        Assert.Equal("PROTO1", result.EsitiPositivi[0].Protocollo);
        Assert.Equal(10, result.EsitiPositivi[0].NInviati);
        Assert.Equal(8, result.EsitiPositivi[0].NAccolti);
    }

    [Fact]
    public async Task EsitoInviiAsync_HttpError_ReturnsFailure()
    {
        var sut = CreateClient(HttpStatusCode.InternalServerError, "error");

        var result = await sut.EsitoInviiAsync(new EsitoInvioRequest
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" }
        });

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task DettaglioErroriAsync_Success_ParsesCsv()
    {
        var csvBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("col1,col2\nval1,val2"));
        var response = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
              <soapenv:Body>
                <ns2:DettaglioErroriResponse xmlns:ns2="http://dettaglioerrori.p730.sanita.sogei.it/">
                  <DatiOutputRichiesta>
                    <esitoChiamata>0</esitoChiamata>
                    <esitiPositivi>
                      <dettagliEsito>
                        <csv>{csvBase64}</csv>
                      </dettagliEsito>
                    </esitiPositivi>
                  </DatiOutputRichiesta>
                </ns2:DettaglioErroriResponse>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
        var sut = CreateClient(HttpStatusCode.OK, response);

        var result = await sut.DettaglioErroriAsync(new DettaglioErroriRequest
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
            Protocollo = "PROTO1"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.CsvContent);
    }

    [Fact]
    public async Task RicevutaPdfAsync_Success_ParsesPdf()
    {
        var pdfBase64 = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var response = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
              <soapenv:Body>
                <ns2:RicevutaPdfResponse xmlns:ns2="http://ricevutapdf.p730.sanita.sogei.it/">
                  <DatiOutputRichiesta>
                    <esitoChiamata>0</esitoChiamata>
                    <esitiPositivi>
                      <dettagliEsito>
                        <pdf>{pdfBase64}</pdf>
                      </dettagliEsito>
                    </esitiPositivi>
                  </DatiOutputRichiesta>
                </ns2:RicevutaPdfResponse>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
        var sut = CreateClient(HttpStatusCode.OK, response);

        var result = await sut.RicevutaPdfAsync(new RicevutaPdfRequest
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
            Protocollo = "PROTO1"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.PdfContent);
        Assert.Equal(4, result.PdfContent!.Length);
    }

    private sealed class FakeCryptoService : ICryptoService
    {
        public string EncryptToBase64(string plainText) => Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
    }

    private sealed class FakeEnvironmentSettings : IEnvironmentSettingsProvider
    {
        public bool IsProduction => false;
        public string InvioEndpointUrl => "https://test.example.com/invio";
        public string EsitoInviiEndpointUrl => "https://test.example.com/esito";
        public string DettaglioErroriEndpointUrl => "https://test.example.com/dettaglio";
        public string RicevutaPdfEndpointUrl => "https://test.example.com/ricevuta";
        public string InterrogazionePuntualeEndpointUrl => "https://test.example.com/interrogazione";
        public string ReportMensileEndpointUrl => "https://test.example.com/report";
        public string DocumentoSpesaEndpointUrl => "https://test.example.com/documento";
        public string DettaglioSegnalazioneEndpointUrl => "https://test.example.com/segnalazione";
        public string ReportSegnalazioniEndpointUrl => "https://test.example.com/report-segnalazioni";
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "text/xml")
            });
        }
    }
}
