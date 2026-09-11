using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using SistemaTs.Core.Dtos;

namespace SistemaTs.Infrastructure.Services;

internal static class SoapTransportHelper
{
    public static async Task<string> SendSoapRequestAsync(
        HttpClient httpClient,
        string endpointUrl,
        string soapEnvelope,
        string soapAction,
        ProviderCredentialsDto credentials,
        CancellationToken ct)
    {
        var authBytes = Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl)
        {
            Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{soapAction}\"");

        var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {body}");

        return body;
    }

    public static string? GetElementValue(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;

    public static IEnumerable<XElement> GetElements(XElement parent, string localName) =>
        parent.Elements().Where(e => e.Name.LocalName == localName);

    public static XElement? FindDescendant(XDocument doc, string localName) =>
        doc.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

    public static string EscapeXml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
