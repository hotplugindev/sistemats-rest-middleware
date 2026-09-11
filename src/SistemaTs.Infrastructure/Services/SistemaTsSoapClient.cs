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
    private const string SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string EjbNs = "http://ejb.invioTelematicoSS730p.sanita.finanze.it/";
    private const string XopNs = "http://www.w3.org/2004/08/xop/include";
    private const string RootContentId = "rootpart@soapui.org";

    private readonly HttpClient _httpClient;
    private readonly SistemaTsOptions _options;

    public SistemaTsSoapClient(HttpClient httpClient, IOptions<SistemaTsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SubmissionResultDto> InviaFileAsync(SoapSubmissionRequest payload, CancellationToken cancellationToken = default)
    {
        var boundary = $"----=_Part_{Random.Shared.Next(1, 999)}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var attachmentContentId = payload.NomeFileAllegato;

        var envelope = BuildSoapEnvelope(payload, attachmentContentId);
        var envelopeBytes = Encoding.UTF8.GetBytes(envelope);

        var body = BuildMultipartBody(boundary, envelopeBytes, payload.ZipContent, attachmentContentId);

        using var content = new ByteArrayContent(body);
        content.Headers.Remove("Content-Type");
        content.Headers.TryAddWithoutValidation("Content-Type",
            $"multipart/related; type=\"application/xop+xml\"; start=\"<{RootContentId}>\"; start-info=\"text/xml\"; boundary=\"{boundary}\"");

        var authBytes = Encoding.UTF8.GetBytes($"{payload.Credentials.Username}:{payload.Credentials.Password}");

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.EndpointUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        request.Headers.TryAddWithoutValidation("SOAPAction", "\"\"");
        request.Headers.TryAddWithoutValidation("MIME-Version", "1.0");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip,deflate");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return SubmissionResultDto.Failure($"HTTP {(int)response.StatusCode}: {responseBody}");
        }

        return ParseSoapResponse(responseBody);
    }

    private static byte[] BuildMultipartBody(string boundary, byte[] envelopeBytes, byte[] zipContent, string attachmentContentId)
    {
        using var ms = new MemoryStream();
        var writer = new StreamWriter(ms, new UTF8Encoding(false), 4096, leaveOpen: true);

        writer.Write($"--{boundary}\r\n");
        writer.Write("Content-Type: application/xop+xml; charset=UTF-8\r\n");
        writer.Write($"Content-Id: <{RootContentId}>\r\n");
        writer.Write("\r\n");
        writer.Flush();
        ms.Write(envelopeBytes);
        writer.Write("\r\n");

        writer.Write($"--{boundary}\r\n");
        writer.Write("Content-Type: application/zip\r\n");
        writer.Write("Content-Transfer-Encoding: binary\r\n");
        writer.Write($"Content-Id: <{attachmentContentId}>\r\n");
        writer.Write("\r\n");
        writer.Flush();
        ms.Write(zipContent);
        writer.Write("\r\n");

        writer.Write($"--{boundary}--\r\n");
        writer.Flush();

        return ms.ToArray();
    }

    private static string BuildSoapEnvelope(SoapSubmissionRequest payload, string contentId)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append($"<soapenv:Envelope xmlns:soapenv=\"{SoapNs}\" xmlns:ejb=\"{EjbNs}\">");
        sb.Append("<soapenv:Header/>");
        sb.Append("<soapenv:Body>");
        sb.Append("<ejb:inviaFileMtom>");
        sb.Append($"<nomeFileAllegato>{EscapeXml(payload.NomeFileAllegato)}</nomeFileAllegato>");
        sb.Append($"<pincodeInvianteCifrato>{EscapeXml(payload.PincodeInvianteCifrato)}</pincodeInvianteCifrato>");

        if (payload.Owner is not null)
        {
            sb.Append("<datiProprietario>");
            if (!string.IsNullOrEmpty(payload.Owner.CodiceRegione))
                sb.Append($"<codiceRegione>{EscapeXml(payload.Owner.CodiceRegione)}</codiceRegione>");
            if (!string.IsNullOrEmpty(payload.Owner.CodiceAsl))
                sb.Append($"<codiceAsl>{EscapeXml(payload.Owner.CodiceAsl)}</codiceAsl>");
            if (!string.IsNullOrEmpty(payload.Owner.CodiceSsa))
                sb.Append($"<codiceSSA>{EscapeXml(payload.Owner.CodiceSsa)}</codiceSSA>");
            if (!string.IsNullOrEmpty(payload.Owner.CfProprietario))
                sb.Append($"<cfProprietario>{EscapeXml(payload.Owner.CfProprietario)}</cfProprietario>");
            sb.Append("</datiProprietario>");
        }

        sb.Append($"<opzionale1>{EscapeXml(payload.Opzionale1 ?? "")}</opzionale1>");
        sb.Append($"<opzionale2>{EscapeXml(payload.Opzionale2 ?? "")}</opzionale2>");
        sb.Append($"<opzionale3>{EscapeXml(payload.Opzionale3 ?? "")}</opzionale3>");
        sb.Append($"<documento><xop:Include xmlns:xop=\"{XopNs}\" href=\"cid:{contentId}\"/></documento>");
        sb.Append("</ejb:inviaFileMtom>");
        sb.Append("</soapenv:Body>");
        sb.Append("</soapenv:Envelope>");

        return sb.ToString();
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static SubmissionResultDto ParseSoapResponse(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);

            var faultEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (faultEl is not null)
            {
                var faultString = faultEl.Elements().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value ?? "Unknown SOAP fault";
                return SubmissionResultDto.Failure($"SOAP Fault: {faultString}");
            }

            var returnEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "return");
            if (returnEl is null)
                return SubmissionResultDto.Failure("Unable to parse SOAP response: no 'return' element found.");

            var codiceEsito = returnEl.Elements().FirstOrDefault(e => e.Name.LocalName == "codiceEsito")?.Value ?? "";
            var descrizioneEsito = returnEl.Elements().FirstOrDefault(e => e.Name.LocalName == "descrizioneEsito")?.Value ?? "";
            var protocollo = returnEl.Elements().FirstOrDefault(e => e.Name.LocalName == "protocollo")?.Value ?? "";
            var dataAccoglienza = returnEl.Elements().FirstOrDefault(e => e.Name.LocalName == "dataAccoglienza")?.Value ?? "";
            var nomeFile = returnEl.Elements().FirstOrDefault(e => e.Name.LocalName == "nomeFileAllegato")?.Value ?? "";
            var dimensioneFile = returnEl.Elements().FirstOrDefault(e => e.Name.LocalName == "dimensioneFileAllegato")?.Value ?? "";
            var idErrore = returnEl.Elements().FirstOrDefault(e => e.Name.LocalName == "idErrore")?.Value;

            var isSuccess = codiceEsito is "0" or "000" or "1";

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
