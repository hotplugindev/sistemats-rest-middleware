using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Configuration;

namespace SistemaTs.Infrastructure.Services;

public sealed class RicevuteSoapClient : SimpleSoapClientBase, IRicevuteClient
{
    private const string EsitoNs = "http://esitoinvio.p730.sanita.sogei.it/";
    private const string DettaglioNs = "http://dettaglioerrori.p730.sanita.sogei.it/";
    private const string RicevutaNs = "http://ricevutapdf.p730.sanita.sogei.it/";

    private readonly SistemaTsOptions _options;
    private readonly ICryptoService _cryptoService;

    public RicevuteSoapClient(HttpClient httpClient, IOptions<SistemaTsOptions> options, ICryptoService cryptoService)
        : base(httpClient)
    {
        _options = options.Value;
        _cryptoService = cryptoService;
    }

    public async Task<EsitoInvioResponseDto> EsitoInviiAsync(EsitoInvioRequest request, CancellationToken ct = default)
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body = $"<esit:EsitoInvii><DatiInputRichiesta>" +
                   $"<pinCode>{pinCode}</pinCode>" +
                   $"{(request.DataInizio is not null ? $"<dataInizio>{request.DataInizio}</dataInizio>" : "")}" +
                   $"{(request.DataFine is not null ? $"<dataFine>{request.DataFine}</dataFine>" : "")}" +
                   $"{(request.Protocollo is not null ? $"<protocollo>{request.Protocollo}</protocollo>" : "")}" +
                   $"</DatiInputRichiesta></esit:EsitoInvii>";

        var envelope = WrapEnvelope(body, "esit", EsitoNs);

        try
        {
            var xml = await PostSoapAsync(_options.EsitoInviiEndpointUrl, "", request.Credentials, envelope, ct);
            return ParseEsitoInviiResponse(xml);
        }
        catch (HttpRequestException ex)
        {
            return new EsitoInvioResponseDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    public async Task<DettaglioErroriResponseDto> DettaglioErroriAsync(DettaglioErroriRequest request, CancellationToken ct = default)
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body = $"<det:DettaglioErrori><DatiInputRichiesta>" +
                   $"<pinCode>{pinCode}</pinCode>" +
                   $"<protocollo>{request.Protocollo}</protocollo>" +
                   $"</DatiInputRichiesta></det:DettaglioErrori>";

        var envelope = WrapEnvelope(body, "det", DettaglioNs);

        try
        {
            var xml = await PostSoapAsync(_options.DettaglioErroriEndpointUrl, "", request.Credentials, envelope, ct);
            return ParseDettaglioErroriResponse(xml);
        }
        catch (HttpRequestException ex)
        {
            return new DettaglioErroriResponseDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    public async Task<RicevutaPdfResponseDto> RicevutaPdfAsync(RicevutaPdfRequest request, CancellationToken ct = default)
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body = $"<ric:RicevutaPdf><DatiInputRichiesta>" +
                   $"<pinCode>{pinCode}</pinCode>" +
                   $"<protocollo>{request.Protocollo}</protocollo>" +
                   $"</DatiInputRichiesta></ric:RicevutaPdf>";

        var envelope = WrapEnvelope(body, "ric", RicevutaNs);

        try
        {
            var xml = await PostSoapAsync(_options.RicevutaPdfEndpointUrl, "", request.Credentials, envelope, ct);
            return ParseRicevutaPdfResponse(xml);
        }
        catch (HttpRequestException ex)
        {
            return new RicevutaPdfResponseDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    private static EsitoInvioResponseDto ParseEsitoInviiResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var outputEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "DatiOutputRichiesta");
        if (outputEl is null)
            return new EsitoInvioResponseDto { Success = false, Errors = new[] { "No DatiOutputRichiesta in response" } };

        var esitoChiamata = GetElementValue(outputEl, "esitoChiamata") ?? "";
        var descrizione = GetElementValue(outputEl, "descrizioneEsito") ?? "";
        var success = esitoChiamata == "0" || esitoChiamata == "000";

        var positivi = new List<EsitoPositivoDto>();
        var positiviEl = GetElement(outputEl, "esitiPositivi");
        if (positiviEl is not null)
        {
            foreach (var d in positiviEl.Elements().Where(e => e.Name.LocalName == "dettagliEsito"))
            {
                positivi.Add(new EsitoPositivoDto
                {
                    Protocollo = GetElementValue(d, "protocollo"),
                    DataInvio = GetElementValue(d, "dataInvio"),
                    Stato = int.TryParse(GetElementValue(d, "stato"), out var s) ? s : 0,
                    Descrizione = GetElementValue(d, "descrizione"),
                    NInviati = long.TryParse(GetElementValue(d, "nInviati"), out var ni) ? ni : 0,
                    NAccolti = long.TryParse(GetElementValue(d, "nAccolti"), out var na) ? na : 0,
                    NWarnings = long.TryParse(GetElementValue(d, "nWarnings"), out var nw) ? nw : 0,
                    NErrori = long.TryParse(GetElementValue(d, "nErrori"), out var ne) ? ne : 0
                });
            }
        }

        var negativi = new List<EsitoNegativoDto>();
        var negativiEl = GetElement(outputEl, "esitiNegativi");
        if (negativiEl is not null)
        {
            foreach (var d in negativiEl.Elements().Where(e => e.Name.LocalName == "dettaglioEsitoNegativo"))
            {
                negativi.Add(new EsitoNegativoDto
                {
                    Codice = GetElementValue(d, "codice"),
                    Descrizione = GetElementValue(d, "descrizione")
                });
            }
        }

        return new EsitoInvioResponseDto
        {
            Success = success,
            EsitoChiamata = esitoChiamata,
            DescrizioneEsito = descrizione,
            EsitiPositivi = positivi,
            EsitiNegativi = negativi,
            Errors = success ? Array.Empty<string>() : new[] { $"[{esitoChiamata}] {descrizione}" }
        };
    }

    private static DettaglioErroriResponseDto ParseDettaglioErroriResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var outputEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "DatiOutputRichiesta");
        if (outputEl is null)
            return new DettaglioErroriResponseDto { Success = false, Errors = new[] { "No DatiOutputRichiesta in response" } };

        var esitoChiamata = GetElementValue(outputEl, "esitoChiamata") ?? "";
        var success = esitoChiamata == "0" || esitoChiamata == "000";

        byte[]? csvContent = null;
        var positiviEl = GetElement(outputEl, "esitiPositivi");
        if (positiviEl is not null)
        {
            var dettagliEl = GetElement(positiviEl, "dettagliEsito");
            if (dettagliEl is not null)
            {
                var csvB64 = GetElementValue(dettagliEl, "csv");
                if (!string.IsNullOrEmpty(csvB64))
                    csvContent = Convert.FromBase64String(csvB64);
            }
        }

        var negativi = new List<EsitoNegativoDto>();
        var negativiEl = GetElement(outputEl, "esitiNegativi");
        if (negativiEl is not null)
        {
            foreach (var d in negativiEl.Elements().Where(e => e.Name.LocalName == "dettaglioEsitoNegativo"))
            {
                negativi.Add(new EsitoNegativoDto
                {
                    Codice = GetElementValue(d, "codice"),
                    Descrizione = GetElementValue(d, "descrizione")
                });
            }
        }

        return new DettaglioErroriResponseDto
        {
            Success = success,
            EsitoChiamata = esitoChiamata,
            CsvContent = csvContent,
            EsitiNegativi = negativi,
            Errors = success ? Array.Empty<string>() : new[] { $"[{esitoChiamata}]" }
        };
    }

    private static RicevutaPdfResponseDto ParseRicevutaPdfResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var outputEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "DatiOutputRichiesta");
        if (outputEl is null)
            return new RicevutaPdfResponseDto { Success = false, Errors = new[] { "No DatiOutputRichiesta in response" } };

        var esitoChiamata = GetElementValue(outputEl, "esitoChiamata") ?? "";
        var success = esitoChiamata == "0" || esitoChiamata == "000";

        byte[]? pdfContent = null;
        var positiviEl = GetElement(outputEl, "esitiPositivi");
        if (positiviEl is not null)
        {
            var dettagliEl = GetElement(positiviEl, "dettagliEsito");
            if (dettagliEl is not null)
            {
                var pdfB64 = GetElementValue(dettagliEl, "pdf");
                if (!string.IsNullOrEmpty(pdfB64))
                    pdfContent = Convert.FromBase64String(pdfB64);
            }
        }

        var negativi = new List<EsitoNegativoDto>();
        var negativiEl = GetElement(outputEl, "esitiNegativi");
        if (negativiEl is not null)
        {
            foreach (var d in negativiEl.Elements().Where(e => e.Name.LocalName == "dettaglioEsitoNegativo"))
            {
                negativi.Add(new EsitoNegativoDto
                {
                    Codice = GetElementValue(d, "codice"),
                    Descrizione = GetElementValue(d, "descrizione")
                });
            }
        }

        return new RicevutaPdfResponseDto
        {
            Success = success,
            EsitoChiamata = esitoChiamata,
            PdfContent = pdfContent,
            EsitiNegativi = negativi,
            Errors = success ? Array.Empty<string>() : new[] { $"[{esitoChiamata}]" }
        };
    }
}
