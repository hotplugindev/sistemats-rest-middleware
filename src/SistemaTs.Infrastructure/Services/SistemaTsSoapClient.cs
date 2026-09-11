using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Configuration;

namespace SistemaTs.Infrastructure.Services;

public sealed class SistemaTsSoapClient : ISistemaTsClient
{
    private static readonly XNamespace SoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Ejb = "http://ejb.invioTelematicoSS730p.sanita.finanze.it/";
    private static readonly XNamespace Xop = "http://www.w3.org/2004/08/xop/include";

    private readonly HttpClient _httpClient;
    private readonly SistemaTsOptions _options;

    public SistemaTsSoapClient(HttpClient httpClient, IOptions<SistemaTsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SubmissionResultDto> InviaFileAsync(SoapSubmissionRequest payload, CancellationToken cancellationToken = default)
    {
        var boundary = $"uuid:{Guid.NewGuid()}";
        var contentId = payload.NomeFileAllegato;

        var envelope = BuildSoapEnvelope(payload, contentId);
        var envelopeBytes = Encoding.UTF8.GetBytes(envelope);

        using var multipartContent = new MultipartContent("related", boundary);
        multipartContent.Headers.ContentType!.Parameters.Add(
            new NameValueHeaderValue("type", "\"application/xop+xml\""));
        multipartContent.Headers.ContentType.Parameters.Add(
            new NameValueHeaderValue("start-info", "\"text/xml\""));

        var soapPart = new ByteArrayContent(envelopeBytes);
        soapPart.Headers.ContentType = new MediaTypeHeaderValue("application/xop+xml");
        soapPart.Headers.ContentType.CharSet = "UTF-8";
        soapPart.Headers.Add("Content-Id", $"<{contentId}-soap>");
        multipartContent.Add(soapPart);

        var attachmentPart = new ByteArrayContent(payload.ZipContent);
        attachmentPart.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        attachmentPart.Headers.Add("Content-Id", $"<{contentId}>");
        attachmentPart.Headers.Add("Content-Transfer-Encoding", "binary");
        multipartContent.Add(attachmentPart);

        var authBytes = Encoding.UTF8.GetBytes($"{payload.Credentials.Username}:{payload.Credentials.Password}");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var requestUri = _options.EndpointUrl;
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = multipartContent
        };
        request.Headers.Add("SOAPAction", "\"\"");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return SubmissionResultDto.Failure($"HTTP {(int)response.StatusCode}: {responseBody}");
        }

        return ParseSoapResponse(responseBody);
    }

    private static string BuildSoapEnvelope(SoapSubmissionRequest payload, string contentId)
    {
        var body = new XElement(Ejb + "inviaFileMtom",
            new XElement("nomeFileAllegato", payload.NomeFileAllegato),
            new XElement("pincodeInvianteCifrato", payload.PincodeInvianteCifrato));

        if (payload.Owner is not null)
        {
            var datiProprietario = new XElement("datiProprietario");
            if (!string.IsNullOrEmpty(payload.Owner.CodiceRegione))
                datiProprietario.Add(new XElement("codiceRegione", payload.Owner.CodiceRegione));
            if (!string.IsNullOrEmpty(payload.Owner.CodiceAsl))
                datiProprietario.Add(new XElement("codiceAsl", payload.Owner.CodiceAsl));
            if (!string.IsNullOrEmpty(payload.Owner.CodiceSsa))
                datiProprietario.Add(new XElement("codiceSSA", payload.Owner.CodiceSsa));
            if (!string.IsNullOrEmpty(payload.Owner.CfProprietario))
                datiProprietario.Add(new XElement("cfProprietario", payload.Owner.CfProprietario));
            body.Add(datiProprietario);
        }

        body.Add(new XElement("opzionale1", payload.Opzionale1 ?? ""));
        body.Add(new XElement("opzionale2", payload.Opzionale2 ?? ""));
        body.Add(new XElement("opzionale3", payload.Opzionale3 ?? ""));

        var documento = new XElement("documento",
            new XElement(Xop + "Include",
                new XAttribute("href", $"cid:{contentId}")));
        body.Add(documento);

        var envelope = new XElement(SoapEnv + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soapenv", SoapEnv.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "ejb", Ejb.NamespaceName),
            new XElement(SoapEnv + "Header"),
            new XElement(SoapEnv + "Body", body));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), envelope);
        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    private static SubmissionResultDto ParseSoapResponse(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            var returnEl = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "return");

            if (returnEl is null)
                return SubmissionResultDto.Failure("Unable to parse SOAP response: no 'return' element found.");

            var codiceEsito = returnEl.Element("codiceEsito")?.Value ?? "";
            var descrizioneEsito = returnEl.Element("descrizioneEsito")?.Value ?? "";
            var protocollo = returnEl.Element("protocollo")?.Value ?? "";
            var dataAccoglienza = returnEl.Element("dataAccoglienza")?.Value ?? "";
            var nomeFile = returnEl.Element("nomeFileAllegato")?.Value ?? "";
            var dimensioneFile = returnEl.Element("dimensioneFileAllegato")?.Value ?? "";
            var idErrore = returnEl.Element("idErrore")?.Value;

            var isSuccess = codiceEsito == "0" || codiceEsito == "1";

            return new SubmissionResultDto
            {
                Success = isSuccess,
                Protocollo = protocollo,
                CodiceEsito = codiceEsito,
                DescrizioneEsito = descrizioneEsito,
                DataAccoglienza = dataAccoglienza,
                NomeFileAllegato = nomeFile,
                DimensioneFileAllegato = dimensioneFile,
                IdErrore = idErrore,
                Errors = isSuccess ? Array.Empty<string>() : new[] { $"[{codiceEsito}] {descrizioneEsito}" }
            };
        }
        catch (Exception ex)
        {
            return SubmissionResultDto.Failure($"Failed to parse SOAP response: {ex.Message}");
        }
    }
}
