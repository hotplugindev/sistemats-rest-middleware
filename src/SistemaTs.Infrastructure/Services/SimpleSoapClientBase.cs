using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using SistemaTs.Core.Dtos;

namespace SistemaTs.Infrastructure.Services;

public abstract class SimpleSoapClientBase
{
    protected readonly HttpClient HttpClient;

    protected SimpleSoapClientBase(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    protected async Task<string> PostSoapAsync(string url, string soapAction, ProviderCredentialsDto credentials, string envelopeXml, CancellationToken ct)
    {
        var authBytes = Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}");

        using var content = new StringContent(envelopeXml, Encoding.UTF8, "text/xml");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{soapAction}\"");

        var response = await HttpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {body}");

        return body;
    }

    protected static string WrapEnvelope(string bodyXml, string nsPrefix, string nsUri)
    {
        return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
               $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:{nsPrefix}=\"{nsUri}\">" +
               $"<soapenv:Header/><soapenv:Body>{bodyXml}</soapenv:Body></soapenv:Envelope>";
    }

    protected static string? GetElementValue(XElement parent, string localName)
        => parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;

    protected static XElement? GetElement(XElement parent, string localName)
        => parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    protected static List<MessaggioDto> ParseMessaggi(XElement? listaEl)
    {
        var result = new List<MessaggioDto>();
        if (listaEl is null) return result;
        foreach (var msg in listaEl.Elements().Where(e => e.Name.LocalName == "messaggio"))
        {
            result.Add(new MessaggioDto
            {
                Codice = GetElementValue(msg, "codice"),
                Descrizione = GetElementValue(msg, "descrizione"),
                Tipo = GetElementValue(msg, "tipo")
            });
        }
        return result;
    }
}
