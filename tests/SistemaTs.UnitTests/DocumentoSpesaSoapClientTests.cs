using System.Net;
using System.Text;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class DocumentoSpesaSoapClientTests
{
    private static readonly string SyncSuccessResponse = """
        <?xml version="1.0" encoding="UTF-8"?>
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
          <soapenv:Body>
            <ns2:inserimentoDocumentoSpesaResponse xmlns:ns2="http://documentospesap730.sanita.finanze.it">
              <esitoChiamata>0</esitoChiamata>
              <protocollo>SYNC_PROTO_1</protocollo>
              <listaMessaggi>
                <messaggio>
                  <codice>INFO01</codice>
                  <descrizione>OK</descrizione>
                  <tipo>I</tipo>
                </messaggio>
              </listaMessaggi>
            </ns2:inserimentoDocumentoSpesaResponse>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    private static DocumentoSpesaSoapClient CreateClient(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var envSettings = new FakeEnvironmentSettings();
        var crypto = new FakeCryptoService();
        return new DocumentoSpesaSoapClient(httpClient, envSettings, crypto);
    }

    private static DocumentoSpesaSyncRequest CreateSyncRequest() => new()
    {
        Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
        Expense = new ExpenseRecordDto
        {
            PIva = "00265910661",
            DataEmissione = new DateOnly(2024, 1, 15),
            Dispositivo = 1,
            NumDocumento = "001",
            DataPagamento = new DateOnly(2024, 1, 15),
            FlagOperazione = "I",
            Items = new List<ExpenseItemDto>
            {
                new() { TipoSpesa = "SR", Importo = 50.00m }
            }
        }
    };

    [Fact]
    public async Task InserimentoAsync_Success_ReturnsProtocollo()
    {
        var sut = CreateClient(HttpStatusCode.OK, SyncSuccessResponse);

        var result = await sut.InserimentoAsync(CreateSyncRequest());

        Assert.True(result.Success);
        Assert.Equal("SYNC_PROTO_1", result.Protocollo);
        Assert.Equal("0", result.EsitoChiamata);
    }

    [Fact]
    public async Task InserimentoAsync_HttpError_ReturnsFailure()
    {
        var sut = CreateClient(HttpStatusCode.InternalServerError, "error");

        var result = await sut.InserimentoAsync(CreateSyncRequest());

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task VariazioneAsync_Success_ReturnsProtocollo()
    {
        var variazioneResponse = SyncSuccessResponse.Replace("inserimentoDocumentoSpesaResponse", "variazioneDocumentoSpesaResponse");
        var sut = CreateClient(HttpStatusCode.OK, variazioneResponse);

        var result = await sut.VariazioneAsync(CreateSyncRequest());

        Assert.True(result.Success);
        Assert.Equal("SYNC_PROTO_1", result.Protocollo);
    }

    [Fact]
    public async Task CancellazioneAsync_Success_ReturnsProtocollo()
    {
        var cancellazioneResponse = SyncSuccessResponse.Replace("inserimentoDocumentoSpesaResponse", "cancellazioneDocumentoSpesaResponse");
        var sut = CreateClient(HttpStatusCode.OK, cancellazioneResponse);

        var result = await sut.CancellazioneAsync(new CancellazioneSincronoRequestDto
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
            PIva = "00265910661",
            DataEmissione = new DateOnly(2024, 1, 15),
            Dispositivo = 1,
            NumDocumento = "001"
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task InserimentoAsync_ErrorResponse_ReturnsMessaggi()
    {
        var errorResponse = """
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
              <soapenv:Body>
                <ns2:inserimentoDocumentoSpesaResponse xmlns:ns2="http://documentospesap730.sanita.finanze.it">
                  <esitoChiamata>1</esitoChiamata>
                  <listaMessaggi>
                    <messaggio>
                      <codice>ERR01</codice>
                      <descrizione>Documento non trovato</descrizione>
                      <tipo>E</tipo>
                    </messaggio>
                  </listaMessaggi>
                </ns2:inserimentoDocumentoSpesaResponse>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
        var sut = CreateClient(HttpStatusCode.OK, errorResponse);

        var result = await sut.InserimentoAsync(CreateSyncRequest());

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("ERR01", result.Errors[0]);
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
