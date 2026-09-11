using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Configuration;

namespace SistemaTs.Infrastructure.Services;

public sealed class InterrogazioniClient : IInterrogazioniClient
{
    private const string InterrogazioneNs = "http://interrogazionepuntuale.p730.sanita.finanze.it";
    private const string ReportMensileNs = "http://reportmensile.p730.sanita.finanze.it";

    private readonly HttpClient _httpClient;
    private readonly SistemaTsOptions _options;
    private readonly ICryptoService _cryptoService;

    public InterrogazioniClient(HttpClient httpClient, IOptions<SistemaTsOptions> options, ICryptoService cryptoService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cryptoService = cryptoService;
    }

    public async Task<InterrogazionePuntualeResponseDto> InterrogazionePuntualeAsync(InterrogazionePuntualeRequest request, CancellationToken ct = default)
    {
        var encryptedPin = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var proprietarioXml = BuildProprietarioXml(request.Owner);

        var envelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:int=""{InterrogazioneNs}"">
<soapenv:Header/>
<soapenv:Body>
<int:interrogazionePuntualeRequest>
<opzionale1></opzionale1>
<opzionale2></opzionale2>
<opzionale3></opzionale3>
<pincode>{SoapTransportHelper.EscapeXml(encryptedPin)}</pincode>
{proprietarioXml}
<idDocumentoFiscale>
<pIva>{SoapTransportHelper.EscapeXml(request.PIva)}</pIva>
<dataEmissione>{request.DataEmissione:yyyy-MM-dd}</dataEmissione>
<numDocumentoFiscale>
<dispositivo>{request.Dispositivo}</dispositivo>
<numDocumento>{SoapTransportHelper.EscapeXml(request.NumDocumento)}</numDocumento>
</numDocumentoFiscale>
</idDocumentoFiscale>
</int:interrogazionePuntualeRequest>
</soapenv:Body>
</soapenv:Envelope>";

        try
        {
            var responseBody = await SoapTransportHelper.SendSoapRequestAsync(
                _httpClient, _options.InterrogazionePuntualeEndpointUrl, envelope,
                InterrogazioneNs, request.Credentials, ct);

            return ParseInterrogazionePuntualeResponse(responseBody);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex.GetType().Name == "SoapTransportException")
        {
            return new InterrogazionePuntualeResponseDto
            {
                Success = false,
                Errors = new[] { ex.Message }
            };
        }
    }

    public async Task<ReportMensileResponseDto> ReportMensileAsync(ReportMensileRequest request, CancellationToken ct = default)
    {
        var encryptedPin = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var proprietarioXml = BuildProprietarioXml(request.Owner);

        var envelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:rep=""{ReportMensileNs}"">
<soapenv:Header/>
<soapenv:Body>
<rep:reportMensileRequest>
<opzionale1></opzionale1>
<opzionale2></opzionale2>
<opzionale3></opzionale3>
<pincode>{SoapTransportHelper.EscapeXml(encryptedPin)}</pincode>
{proprietarioXml}
<annoMese>{SoapTransportHelper.EscapeXml(request.AnnoMese)}</annoMese>
<tipoEstrazione>{SoapTransportHelper.EscapeXml(request.TipoEstrazione)}</tipoEstrazione>
</rep:reportMensileRequest>
</soapenv:Body>
</soapenv:Envelope>";

        try
        {
            var responseBody = await SoapTransportHelper.SendSoapRequestAsync(
                _httpClient, _options.ReportMensileEndpointUrl, envelope,
                ReportMensileNs, request.Credentials, ct);

            return ParseReportMensileResponse(responseBody);
        }
        catch (HttpRequestException ex)
        {
            return new ReportMensileResponseDto
            {
                Success = false,
                Errors = new[] { ex.Message }
            };
        }
    }

    private static string BuildProprietarioXml(OwnerDto? owner)
    {
        if (owner is null) return "";

        var parts = new List<string> { "<Proprietario>" };
        if (!string.IsNullOrEmpty(owner.CodiceRegione))
            parts.Add($"<codiceRegione>{SoapTransportHelper.EscapeXml(owner.CodiceRegione)}</codiceRegione>");
        if (!string.IsNullOrEmpty(owner.CodiceAsl))
            parts.Add($"<codiceAsl>{SoapTransportHelper.EscapeXml(owner.CodiceAsl)}</codiceAsl>");
        if (!string.IsNullOrEmpty(owner.CodiceSsa))
            parts.Add($"<codiceSSA>{SoapTransportHelper.EscapeXml(owner.CodiceSsa)}</codiceSSA>");
        if (!string.IsNullOrEmpty(owner.CfProprietario))
            parts.Add($"<cfProprietario>{SoapTransportHelper.EscapeXml(owner.CfProprietario)}</cfProprietario>");
        parts.Add("</Proprietario>");

        return string.Join("", parts);
    }

    private static InterrogazionePuntualeResponseDto ParseInterrogazionePuntualeResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var responseEl = SoapTransportHelper.FindDescendant(doc, "interrogazionePuntualeResponse");
        if (responseEl is null)
            return new InterrogazionePuntualeResponseDto { Success = false, Errors = new[] { "No response element found" } };

        var esitoChiamata = SoapTransportHelper.GetElementValue(responseEl, "esitoChiamata") ?? "";
        var messaggi = ParseMessaggi(responseEl);

        DocumentoFiscaleDto? documentoFiscale = null;
        var docEl = SoapTransportHelper.GetElements(responseEl, "documentoFiscale").FirstOrDefault();
        if (docEl is not null)
        {
            documentoFiscale = new DocumentoFiscaleDto
            {
                Protocollo = SoapTransportHelper.GetElementValue(docEl, "protocollo"),
                NomeFile = SoapTransportHelper.GetElementValue(docEl, "nomeFile"),
                DataInvio = SoapTransportHelper.GetElementValue(docEl, "dataInvio"),
                TipoInvio = SoapTransportHelper.GetElementValue(docEl, "tipoInvio"),
                DataPagamento = SoapTransportHelper.GetElementValue(docEl, "dataPagamento")
            };

            var idDocEl = SoapTransportHelper.GetElements(docEl, "idDocumentoFiscale").FirstOrDefault();
            if (idDocEl is not null)
            {
                documentoFiscale.PIva = SoapTransportHelper.GetElementValue(idDocEl, "pIva");
                documentoFiscale.DataEmissione = SoapTransportHelper.GetElementValue(idDocEl, "dataEmissione");
                var numDocEl = SoapTransportHelper.GetElements(idDocEl, "numDocumentoFiscale").FirstOrDefault();
                if (numDocEl is not null)
                {
                    documentoFiscale.Dispositivo = int.TryParse(SoapTransportHelper.GetElementValue(numDocEl, "dispositivo"), out var d) ? d : 0;
                    documentoFiscale.NumDocumento = SoapTransportHelper.GetElementValue(numDocEl, "numDocumento");
                }
            }

            foreach (var totale in SoapTransportHelper.GetElements(docEl, "totaliVociSpesa"))
            {
                documentoFiscale.TotaliVociSpesa.Add(new TotaleVoceSpesaDto
                {
                    TipoSpesa = SoapTransportHelper.GetElementValue(totale, "tipoSpesa"),
                    Importo = double.TryParse(SoapTransportHelper.GetElementValue(totale, "importo"), out var i) ? i : 0
                });
            }

            foreach (var totale in SoapTransportHelper.GetElements(docEl, "totaliVociSpesaRimborsate"))
            {
                documentoFiscale.TotaliVociSpesaRimborsate.Add(new TotaleVoceSpesaDto
                {
                    TipoSpesa = SoapTransportHelper.GetElementValue(totale, "tipoSpesa"),
                    Importo = double.TryParse(SoapTransportHelper.GetElementValue(totale, "importo"), out var i) ? i : 0
                });
            }

            var listaErroriEl = SoapTransportHelper.GetElements(docEl, "listaErroriDocumento").FirstOrDefault();
            if (listaErroriEl is not null)
            {
                foreach (var errore in SoapTransportHelper.GetElements(listaErroriEl, "errore"))
                {
                    documentoFiscale.ListaErroriDocumento.Add(new ErroreDocumentoDto
                    {
                        Codice = SoapTransportHelper.GetElementValue(errore, "codice"),
                        Descrizione = SoapTransportHelper.GetElementValue(errore, "descrizione"),
                        Tipo = SoapTransportHelper.GetElementValue(errore, "tipo")
                    });
                }
            }
        }

        var isSuccess = esitoChiamata == "0" || esitoChiamata == "000";

        return new InterrogazionePuntualeResponseDto
        {
            Success = isSuccess,
            EsitoChiamata = esitoChiamata,
            DocumentoFiscale = documentoFiscale,
            ListaMessaggi = messaggi,
            Errors = isSuccess ? Array.Empty<string>() : messaggi.Select(m => $"[{m.Codice}] {m.Descrizione}").ToList()
        };
    }

    private static ReportMensileResponseDto ParseReportMensileResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var responseEl = SoapTransportHelper.FindDescendant(doc, "reportMensileResponse");
        if (responseEl is null)
            return new ReportMensileResponseDto { Success = false, Errors = new[] { "No response element found" } };

        var esitoChiamata = SoapTransportHelper.GetElementValue(responseEl, "esitoChiamata") ?? "";
        var messaggi = ParseMessaggi(responseEl);

        byte[]? csvContent = null;
        var fileCsvBase64 = SoapTransportHelper.GetElementValue(responseEl, "fileCSV");
        if (!string.IsNullOrEmpty(fileCsvBase64))
            csvContent = Convert.FromBase64String(fileCsvBase64);

        var isSuccess = esitoChiamata == "0" || esitoChiamata == "000";

        return new ReportMensileResponseDto
        {
            Success = isSuccess,
            EsitoChiamata = esitoChiamata,
            CsvContent = csvContent,
            ListaMessaggi = messaggi,
            Errors = isSuccess ? Array.Empty<string>() : messaggi.Select(m => $"[{m.Codice}] {m.Descrizione}").ToList()
        };
    }

    internal static List<MessaggioDto> ParseMessaggi(XElement parent)
    {
        var messaggi = new List<MessaggioDto>();
        var listaEl = SoapTransportHelper.GetElements(parent, "listaMessaggi").FirstOrDefault();
        if (listaEl is not null)
        {
            foreach (var msg in SoapTransportHelper.GetElements(listaEl, "messaggio"))
            {
                messaggi.Add(new MessaggioDto
                {
                    Codice = SoapTransportHelper.GetElementValue(msg, "codice"),
                    Descrizione = SoapTransportHelper.GetElementValue(msg, "descrizione"),
                    Tipo = SoapTransportHelper.GetElementValue(msg, "tipo")
                });
            }
        }
        return messaggi;
    }
}
