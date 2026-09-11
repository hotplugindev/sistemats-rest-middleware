using System.Net;
using System.Text;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class SoapClientTests
{
    private static readonly string SuccessResponse = """
        <?xml version="1.0" encoding="UTF-8"?>
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
          <soapenv:Body>
            <ns2:inviaFileMtomResponse xmlns:ns2="http://ejb.invioTelematicoSS730p.sanita.finanze.it/">
              <return>
                <codiceEsito>0</codiceEsito>
                <dataAccoglienza>2024-01-15T10:30:00</dataAccoglienza>
                <descrizioneEsito>File ricevuto con successo</descrizioneEsito>
                <dimensioneFileAllegato>1024</dimensioneFileAllegato>
                <nomeFileAllegato>provaMedico.zip</nomeFileAllegato>
                <protocollo>PROTO123456</protocollo>
              </return>
            </ns2:inviaFileMtomResponse>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    private static SistemaTsSoapClient CreateClient(HttpStatusCode statusCode, string responseBody, string? endpointUrl = null)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var envSettings = new FakeEnvironmentSettingsProvider(endpointUrl ?? "https://invioSS730pTest.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort");
        return new SistemaTsSoapClient(httpClient, envSettings);
    }

    [Fact]
    public async Task InviaFileAsync_SuccessfulResponse_ReturnsProtocollo()
    {
        var sut = CreateClient(HttpStatusCode.OK, SuccessResponse);

        var payload = new SoapSubmissionRequest
        {
            NomeFileAllegato = "provaMedico.zip",
            PincodeInvianteCifrato = "ENCRYPTED_PIN",
            Owner = new OwnerDto { CfProprietario = "PROVAX00X00X000Y" },
            ZipContent = Encoding.UTF8.GetBytes("fake zip content"),
            Credentials = new ProviderCredentialsDto
            {
                Username = "PROVAX00X00X000Y",
                Password = "Salve123",
                Pincode = "1234567890"
            }
        };

        var result = await sut.InviaFileAsync(payload);

        Assert.True(result.Success);
        Assert.Equal("PROTO123456", result.Protocollo);
        Assert.Equal("0", result.CodiceEsito);
        Assert.Equal("provaMedico.zip", result.NomeFileAllegato);
    }

    [Fact]
    public async Task InviaFileAsync_HttpError_ReturnsFailure()
    {
        var sut = CreateClient(HttpStatusCode.InternalServerError, "Server Error");

        var payload = new SoapSubmissionRequest
        {
            NomeFileAllegato = "test.zip",
            PincodeInvianteCifrato = "ENC",
            ZipContent = new byte[] { 1, 2, 3 },
            Credentials = new ProviderCredentialsDto
            {
                Username = "USER",
                Password = "PASS",
                Pincode = "PIN"
            }
        };

        var result = await sut.InviaFileAsync(payload);

        Assert.False(result.Success);
        Assert.Contains("500", result.Errors.First());
    }

    [Fact]
    public async Task InviaFileAsync_SendsBasicAuthHeader()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var envSettings = new FakeEnvironmentSettingsProvider("https://example.com/test");
        var sut = new SistemaTsSoapClient(httpClient, envSettings);

        var payload = new SoapSubmissionRequest
        {
            NomeFileAllegato = "test.zip",
            PincodeInvianteCifrato = "ENC",
            ZipContent = new byte[] { 1 },
            Credentials = new ProviderCredentialsDto
            {
                Username = "MYUSER",
                Password = "MYPASS",
                Pincode = "PIN"
            }
        };

        await sut.InviaFileAsync(payload);

        Assert.NotNull(handler.LastRequest);
        var expectedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("MYUSER:MYPASS"));
        Assert.Equal("Basic", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal(expectedAuth, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task InviaFileAsync_SendsMultipartContentType()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var envSettings = new FakeEnvironmentSettingsProvider("https://example.com/test");
        var sut = new SistemaTsSoapClient(httpClient, envSettings);

        var payload = new SoapSubmissionRequest
        {
            NomeFileAllegato = "test.zip",
            PincodeInvianteCifrato = "ENC",
            ZipContent = new byte[] { 1 },
            Credentials = new ProviderCredentialsDto
            {
                Username = "U",
                Password = "P",
                Pincode = "PIN"
            }
        };

        await sut.InviaFileAsync(payload);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("multipart", handler.LastRequest!.Content!.Headers.ContentType!.MediaType!.Split('/')[0]);
    }

    [Fact]
    public async Task InviaFileAsync_SoapFault_ReturnsFailure()
    {
        var faultResponse = """
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
              <soapenv:Body>
                <soapenv:Fault>
                  <faultcode>soapenv:Client</faultcode>
                  <faultstring>Internal Error</faultstring>
                </soapenv:Fault>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
        var sut = CreateClient(HttpStatusCode.OK, faultResponse);

        var payload = new SoapSubmissionRequest
        {
            NomeFileAllegato = "test.zip",
            PincodeInvianteCifrato = "ENC",
            ZipContent = new byte[] { 1 },
            Credentials = new ProviderCredentialsDto
            {
                Username = "U",
                Password = "P",
                Pincode = "PIN"
            }
        };

        var result = await sut.InviaFileAsync(payload);

        Assert.False(result.Success);
        Assert.Contains("SOAP Fault", result.Errors.First());
    }

    [Fact]
    public async Task InviaFileAsync_ErrorCodiceEsito_ReturnsFailureWithErrors()
    {
        var errorResponse = """
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
              <soapenv:Body>
                <ns2:inviaFileMtomResponse xmlns:ns2="http://ejb.invioTelematicoSS730p.sanita.finanze.it/">
                  <return>
                    <codiceEsito>2</codiceEsito>
                    <descrizioneEsito>Errore validazione</descrizioneEsito>
                    <protocollo></protocollo>
                  </return>
                </ns2:inviaFileMtomResponse>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
        var sut = CreateClient(HttpStatusCode.OK, errorResponse);

        var payload = new SoapSubmissionRequest
        {
            NomeFileAllegato = "test.zip",
            PincodeInvianteCifrato = "ENC",
            ZipContent = new byte[] { 1 },
            Credentials = new ProviderCredentialsDto
            {
                Username = "U",
                Password = "P",
                Pincode = "PIN"
            }
        };

        var result = await sut.InviaFileAsync(payload);

        Assert.False(result.Success);
        Assert.Equal("2", result.CodiceEsito);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task InviaFileAsync_UsesCorrectEndpoint()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var envSettings = new FakeEnvironmentSettingsProvider("https://custom-endpoint.example.com/soap");
        var sut = new SistemaTsSoapClient(httpClient, envSettings);

        var payload = new SoapSubmissionRequest
        {
            NomeFileAllegato = "test.zip",
            PincodeInvianteCifrato = "ENC",
            ZipContent = new byte[] { 1 },
            Credentials = new ProviderCredentialsDto
            {
                Username = "U",
                Password = "P",
                Pincode = "PIN"
            }
        };

        await sut.InviaFileAsync(payload);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://custom-endpoint.example.com/soap", handler.LastRequest!.RequestUri!.ToString());
    }

    private sealed class FakeEnvironmentSettingsProvider : IEnvironmentSettingsProvider
    {
        private readonly string _invioUrl;

        public FakeEnvironmentSettingsProvider(string invioUrl)
        {
            _invioUrl = invioUrl;
        }

        public bool IsProduction => false;
        public string InvioEndpointUrl => _invioUrl;
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

        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "text/xml")
            };
            return Task.FromResult(response);
        }
    }
}
