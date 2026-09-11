using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SistemaTs.Core.Dtos;
using SistemaTs.Infrastructure.Configuration;
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

    private static SistemaTsSoapClient CreateClient(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new SistemaTsOptions
        {
            EndpointUrl = "https://invioSS730pTest.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort"
        });
        return new SistemaTsSoapClient(httpClient, options);
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
        var options = Options.Create(new SistemaTsOptions
        {
            EndpointUrl = "https://example.com/test"
        });
        var sut = new SistemaTsSoapClient(httpClient, options);

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
        var options = Options.Create(new SistemaTsOptions
        {
            EndpointUrl = "https://example.com/test"
        });
        var sut = new SistemaTsSoapClient(httpClient, options);

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
