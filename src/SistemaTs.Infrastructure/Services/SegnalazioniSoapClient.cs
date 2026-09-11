using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Configuration;

namespace SistemaTs.Infrastructure.Services;

public sealed class SegnalazioniSoapClient : SimpleSoapClientBase, ISegnalazioniClient
{
    private const string DettaglioSegNs = "http://dettagliosegnalazione.p730.sanita.finanze.it";
    private const string ReportSegNs = "http://reportsegnalazioni.p730.sanita.finanze.it";

    private readonly SistemaTsOptions _options;
    private readonly ICryptoService _cryptoService;

    public SegnalazioniSoapClient(HttpClient httpClient, IOptions<SistemaTsOptions> options, ICryptoService cryptoService)
        : base(httpClient)
    {
        _options = options.Value;
        _cryptoService = cryptoService;
    }

    public async Task<DettaglioSegnalazioneResponseDto> DettaglioSegnalazioneAsync(DettaglioSegnalazioneRequest request, CancellationToken ct = default)
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body = $"<seg:dettaglioSegnalazioneRequest>" +
                   $"<pincode>{pinCode}</pincode>" +
                   $"<Proprietario>";
        if (!string.IsNullOrEmpty(request.Owner.CodiceRegione)) body += $"<codiceRegione>{request.Owner.CodiceRegione}</codiceRegione>";
        if (!string.IsNullOrEmpty(request.Owner.CodiceAsl)) body += $"<codiceAsl>{request.Owner.CodiceAsl}</codiceAsl>";
        if (!string.IsNullOrEmpty(request.Owner.CodiceSsa)) body += $"<codiceSSA>{request.Owner.CodiceSsa}</codiceSSA>";
        body += $"<cfProprietario>{request.Owner.CfProprietario}</cfProprietario></Proprietario>" +
                $"<idDocumentoFiscale>" +
                $"<pIva>{request.PIva}</pIva>" +
                $"<dataEmissione>{request.DataEmissione:yyyy-MM-dd}</dataEmissione>" +
                $"<numDocumentoFiscale><dispositivo>{request.Dispositivo}</dispositivo><numDocumento>{request.NumDocumento}</numDocumento></numDocumentoFiscale>" +
                $"</idDocumentoFiscale>" +
                $"</seg:dettaglioSegnalazioneRequest>";

        var envelope = WrapEnvelope(body, "seg", DettaglioSegNs);

        try
        {
            var xml = await PostSoapAsync(_options.DettaglioSegnalazioneEndpointUrl, "dettagliosegnalazione.p730.sanita.finanze.it", request.Credentials, envelope, ct);
            return ParseDettaglioSegnalazioneResponse(xml);
        }
        catch (HttpRequestException ex)
        {
            return new DettaglioSegnalazioneResponseDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    public async Task<ReportSegnalazioniResponseDto> ReportSegnalazioniAsync(ReportSegnalazioniRequest request, CancellationToken ct = default)
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body = $"<rep:reportSegnalazioniRequest>" +
                   $"<pincode>{pinCode}</pincode>" +
                   $"<Proprietario>";
        if (!string.IsNullOrEmpty(request.Owner.CodiceRegione)) body += $"<codiceRegione>{request.Owner.CodiceRegione}</codiceRegione>";
        if (!string.IsNullOrEmpty(request.Owner.CodiceAsl)) body += $"<codiceAsl>{request.Owner.CodiceAsl}</codiceAsl>";
        if (!string.IsNullOrEmpty(request.Owner.CodiceSsa)) body += $"<codiceSSA>{request.Owner.CodiceSsa}</codiceSSA>";
        body += $"<cfProprietario>{request.Owner.CfProprietario}</cfProprietario></Proprietario>" +
                $"<dataIniPeriodoSegnalazione>{request.DataIniPeriodoSegnalazione:yyyy-MM-dd}</dataIniPeriodoSegnalazione>" +
                $"<dataFinPeriodoSegnalazione>{request.DataFinPeriodoSegnalazione:yyyy-MM-dd}</dataFinPeriodoSegnalazione>" +
                $"</rep:reportSegnalazioniRequest>";

        var envelope = WrapEnvelope(body, "rep", ReportSegNs);

        try
        {
            var xml = await PostSoapAsync(_options.ReportSegnalazioniEndpointUrl, "reportsegnalazioni.p730.sanita.finanze.it", request.Credentials, envelope, ct);
            return ParseReportSegnalazioniResponse(xml);
        }
        catch (HttpRequestException ex)
        {
            return new ReportSegnalazioniResponseDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    private static DettaglioSegnalazioneResponseDto ParseDettaglioSegnalazioneResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var responseEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "dettaglioSegnalazioneResponse");
        if (responseEl is null)
            return new DettaglioSegnalazioneResponseDto { Success = false, Errors = new[] { "No dettaglioSegnalazioneResponse in response" } };

        var esitoChiamata = GetElementValue(responseEl, "esitoChiamata") ?? "";
        var success = esitoChiamata == "0" || esitoChiamata == "000";

        DocumentoSegnalazioneDto? documento = null;
        var docEl = GetElement(responseEl, "documentoFiscale");
        if (docEl is not null)
        {
            documento = new DocumentoSegnalazioneDto();
            var idEl = GetElement(docEl, "idDocumentoFiscale");
            if (idEl is not null)
            {
                documento.PIva = GetElementValue(idEl, "pIva");
                documento.DataEmissione = GetElementValue(idEl, "dataEmissione");
                var numEl = GetElement(idEl, "numDocumentoFiscale");
                if (numEl is not null)
                {
                    documento.Dispositivo = int.TryParse(GetElementValue(numEl, "dispositivo"), out var d) ? d : 0;
                    documento.NumDocumento = GetElementValue(numEl, "numDocumento");
                }
            }
            documento.DataPagamento = GetElementValue(docEl, "dataPagamento");
            documento.DataInvio = GetElementValue(docEl, "dataInvio");
            documento.Protocollo = GetElementValue(docEl, "protocollo");
            documento.NomeFile = GetElementValue(docEl, "nomeFile");

            var listaSegEl = GetElement(docEl, "listaSegnalazioniDocumento");
            if (listaSegEl is not null)
            {
                foreach (var seg in listaSegEl.Elements().Where(e => e.Name.LocalName == "segnalazione"))
                    documento.ListaSegnalazioniDocumento.Add(seg.Value);
            }

            foreach (var voce in docEl.Elements().Where(e => e.Name.LocalName == "listaVociSpesa"))
            {
                documento.ListaVociSpesa.Add(new VoceSpesaSegnalazioneDto
                {
                    TipoSpesa = GetElementValue(voce, "tipoSpesa"),
                    Importo = double.TryParse(GetElementValue(voce, "importo"), out var imp) ? imp : 0,
                    SegnalazioneImporto = GetElementValue(voce, "segnalazioneImporto"),
                    SegnalazioneTipoVoceSpesa = GetElementValue(voce, "segnalazioneTipoVoceSpesa")
                });
            }
            foreach (var voce in docEl.Elements().Where(e => e.Name.LocalName == "listaVociSpesaRimborsate"))
            {
                documento.ListaVociSpesaRimborsate.Add(new VoceSpesaSegnalazioneDto
                {
                    TipoSpesa = GetElementValue(voce, "tipoSpesa"),
                    Importo = double.TryParse(GetElementValue(voce, "importo"), out var imp) ? imp : 0,
                    SegnalazioneImporto = GetElementValue(voce, "segnalazioneImporto"),
                    SegnalazioneTipoVoceSpesa = GetElementValue(voce, "segnalazioneTipoVoceSpesa")
                });
            }
        }

        var messaggi = ParseMessaggi(GetElement(responseEl, "listaMessaggi"));

        return new DettaglioSegnalazioneResponseDto
        {
            Success = success,
            EsitoChiamata = esitoChiamata,
            DocumentoFiscale = documento,
            ListaMessaggi = messaggi,
            Errors = success ? Array.Empty<string>() : messaggi.Where(m => m.Tipo == "E").Select(m => $"[{m.Codice}] {m.Descrizione}").ToList()
        };
    }

    private static ReportSegnalazioniResponseDto ParseReportSegnalazioniResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var responseEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "reportSegnalazioniResponse");
        if (responseEl is null)
            return new ReportSegnalazioniResponseDto { Success = false, Errors = new[] { "No reportSegnalazioniResponse in response" } };

        var esitoChiamata = GetElementValue(responseEl, "esitoChiamata") ?? "";
        var success = esitoChiamata == "0" || esitoChiamata == "000";

        byte[]? csvContent = null;
        var csvB64 = GetElementValue(responseEl, "fileCSV");
        if (!string.IsNullOrEmpty(csvB64))
            csvContent = Convert.FromBase64String(csvB64);

        var messaggi = ParseMessaggi(GetElement(responseEl, "listaMessaggi"));

        return new ReportSegnalazioniResponseDto
        {
            Success = success,
            EsitoChiamata = esitoChiamata,
            CsvContent = csvContent,
            ListaMessaggi = messaggi,
            Errors = success ? Array.Empty<string>() : messaggi.Where(m => m.Tipo == "E").Select(m => $"[{m.Codice}] {m.Descrizione}").ToList()
        };
    }
}
