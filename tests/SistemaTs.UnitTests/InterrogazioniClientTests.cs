using System.Net;
using System.Text;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class InterrogazioniClientTests
{
    private static readonly string InterrogazioneSuccessResponse = """
        <?xml version="1.0" encoding="UTF-8"?>
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
          <soapenv:Body>
            <ns2:interrogazionePuntualeResponse xmlns:ns2="http://interrogazionepuntuale.p730.sanita.finanze.it">
              <esitoChiamata>0</esitoChiamata>
              <documentoFiscale>
                <idDocumentoFiscale>
                  <pIva>00265910661</pIva>
                  <dataEmissione>2024-01-15</dataEmissione>
                  <numDocumentoFiscale>
                    <dispositivo>1</dispositivo>
                    <numDocumento>001</numDocumento>
                  </numDocumentoFiscale>
                </idDocumentoFiscale>
                <dataPagamento>2024-01-15</dataPagamento>
                <protocollo>PROTO_INT_1</protocollo>
                <nomeFile>file.zip</nomeFile>
                <dataInvio>2024-01-16</dataInvio>
                <tipoInvio>1</tipoInvio>
                <totaliVociSpesa>
                  <tipoSpesa>SR</tipoSpesa>
                  <importo>52.01</importo>
                </totaliVociSpesa>
              </documentoFiscale>
              <listaMessaggi>
                <messaggio>
                  <codice>INFO</codice>
                  <descrizione>OK</descrizione>
                  <tipo>I</tipo>
                </messaggio>
              </listaMessaggi>
            </ns2:interrogazionePuntualeResponse>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    private static InterrogazioniClient CreateClient(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var envSettings = new FakeEnvironmentSettings();
        var crypto = new FakeCryptoService();
        return new InterrogazioniClient(httpClient, envSettings, crypto);
    }

    [Fact]
    public async Task InterrogazionePuntualeAsync_Success_ParsesDocumento()
    {
        var sut = CreateClient(HttpStatusCode.OK, InterrogazioneSuccessResponse);

        var result = await sut.InterrogazionePuntualeAsync(new InterrogazionePuntualeRequest
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
            PIva = "00265910661",
            DataEmissione = new DateOnly(2024, 1, 15),
            Dispositivo = 1,
            NumDocumento = "001"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.DocumentoFiscale);
        Assert.Equal("PROTO_INT_1", result.DocumentoFiscale!.Protocollo);
        Assert.Equal("00265910661", result.DocumentoFiscale.PIva);
    }

    [Fact]
    public async Task InterrogazionePuntualeAsync_HttpError_ReturnsFailure()
    {
        var sut = CreateClient(HttpStatusCode.InternalServerError, "error");

        var result = await sut.InterrogazionePuntualeAsync(new InterrogazionePuntualeRequest
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
            PIva = "00265910661",
            DataEmissione = new DateOnly(2024, 1, 15),
            NumDocumento = "001"
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReportMensileAsync_Success_ParsesCsv()
    {
        var csvBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("data"));
        var response = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
              <soapenv:Body>
                <ns2:reportMensileResponse xmlns:ns2="http://reportmensile.p730.sanita.finanze.it">
                  <esitoChiamata>0</esitoChiamata>
                  <fileCSV>{csvBase64}</fileCSV>
                  <listaMessaggi/>
                </ns2:reportMensileResponse>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
        var sut = CreateClient(HttpStatusCode.OK, response);

        var result = await sut.ReportMensileAsync(new ReportMensileRequest
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
            AnnoMese = "202401",
            TipoEstrazione = "1"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.CsvContent);
    }

    [Fact]
    public async Task ReportMensileAsync_HttpError_ReturnsFailure()
    {
        var sut = CreateClient(HttpStatusCode.BadGateway, "bad gateway");

        var result = await sut.ReportMensileAsync(new ReportMensileRequest
        {
            Credentials = new ProviderCredentialsDto { Username = "U", Password = "P", Pincode = "PIN" },
            AnnoMese = "202401",
            TipoEstrazione = "1"
        });

        Assert.False(result.Success);
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
