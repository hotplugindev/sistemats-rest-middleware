using System.Xml.Linq;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Infrastructure.Services;

public sealed class DocumentoSpesaSoapClient : SimpleSoapClientBase, IDocumentoSpesaClient
{
    private const string DocSpesaNs = "http://documentospesap730.sanita.finanze.it";

    private readonly IEnvironmentSettingsProvider _environmentSettings;
    private readonly ICryptoService _cryptoService;

    public DocumentoSpesaSoapClient(
        HttpClient httpClient,
        IEnvironmentSettingsProvider environmentSettings,
        ICryptoService cryptoService
    )
        : base(httpClient)
    {
        _environmentSettings = environmentSettings;
        _cryptoService = cryptoService;
    }

    public async Task<SincronoResultDto> InserimentoAsync(
        DocumentoSpesaSyncRequest request,
        CancellationToken ct = default
    )
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body = BuildSyncBody(
            pinCode,
            request.Owner,
            request.Expense,
            "inserimentoDocumentoSpesaRequest",
            "idInserimentoDocumentoFiscale"
        );
        var envelope = WrapEnvelope(body, "doc", DocSpesaNs);

        try
        {
            var xml = await PostSoapAsync(
                _environmentSettings.DocumentoSpesaEndpointUrl,
                "inserimento.documentospesap730.sanita.finanze.it",
                request.Credentials,
                envelope,
                ct
            );
            return ParseSyncResponse(xml, "inserimentoDocumentoSpesaResponse");
        }
        catch (HttpRequestException ex)
        {
            return new SincronoResultDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    public async Task<SincronoResultDto> VariazioneAsync(
        DocumentoSpesaSyncRequest request,
        CancellationToken ct = default
    )
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body = BuildSyncBody(
            pinCode,
            request.Owner,
            request.Expense,
            "variazioneDocumentoSpesaRequest",
            "idVariazioneDocumentoFiscale"
        );
        var envelope = WrapEnvelope(body, "doc", DocSpesaNs);

        try
        {
            var xml = await PostSoapAsync(
                _environmentSettings.DocumentoSpesaEndpointUrl,
                "variazione.documentospesap730.sanita.finanze.it",
                request.Credentials,
                envelope,
                ct
            );
            return ParseSyncResponse(xml, "variazioneDocumentoSpesaResponse");
        }
        catch (HttpRequestException ex)
        {
            return new SincronoResultDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    public async Task<SincronoResultDto> RimborsoAsync(
        RimborsoSincronoRequestDto request,
        CancellationToken ct = default
    )
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body =
            $"<doc:rimborsoDocumentoSpesaRequest>"
            + $"<pincode>{pinCode}</pincode>"
            + BuildProprietarioXml(request.Owner)
            + $"<idRimborsoDocumentoFiscale>"
            + $"<pIva>{request.PIvaOriginale}</pIva>"
            + $"<dataEmissione>{request.DataEmissioneOriginale:yyyy-MM-dd}</dataEmissione>"
            + $"<numDocumentoFiscale><dispositivo>{request.DispositivoOriginale}</dispositivo><numDocumento>{request.NumDocumentoOriginale}</numDocumento></numDocumentoFiscale>"
            + $"</idRimborsoDocumentoFiscale>"
            + $"<DocumentoSpesa>{BuildDocumentoSpesaXml(request.Expense)}</DocumentoSpesa>"
            + $"</doc:rimborsoDocumentoSpesaRequest>";

        var envelope = WrapEnvelope(body, "doc", DocSpesaNs);

        try
        {
            var xml = await PostSoapAsync(
                _environmentSettings.DocumentoSpesaEndpointUrl,
                "rimborso.documentospesap730.sanita.finanze.it",
                request.Credentials,
                envelope,
                ct
            );
            return ParseSyncResponse(xml, "rimborsoDocumentoSpesaResponse");
        }
        catch (HttpRequestException ex)
        {
            return new SincronoResultDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    public async Task<SincronoResultDto> CancellazioneAsync(
        CancellazioneSincronoRequestDto request,
        CancellationToken ct = default
    )
    {
        var pinCode = _cryptoService.EncryptToBase64(request.Credentials.Pincode!);
        var body =
            $"<doc:cancellazioneDocumentoSpesaRequest>"
            + $"<pincode>{pinCode}</pincode>"
            + BuildProprietarioXml(request.Owner)
            + $"<idCancellazioneDocumentoFiscale>"
            + $"<pIva>{request.PIva}</pIva>"
            + $"<dataEmissione>{request.DataEmissione:yyyy-MM-dd}</dataEmissione>"
            + $"<numDocumentoFiscale><dispositivo>{request.Dispositivo}</dispositivo><numDocumento>{request.NumDocumento}</numDocumento></numDocumentoFiscale>"
            + $"</idCancellazioneDocumentoFiscale>"
            + $"</doc:cancellazioneDocumentoSpesaRequest>";

        var envelope = WrapEnvelope(body, "doc", DocSpesaNs);

        try
        {
            var xml = await PostSoapAsync(
                _environmentSettings.DocumentoSpesaEndpointUrl,
                "cancellazione.documentospesap730.sanita.finanze.it",
                request.Credentials,
                envelope,
                ct
            );
            return ParseSyncResponse(xml, "cancellazioneDocumentoSpesaResponse");
        }
        catch (HttpRequestException ex)
        {
            return new SincronoResultDto { Success = false, Errors = new[] { ex.Message } };
        }
    }

    private static string BuildSyncBody(
        string pinCode,
        OwnerDto? owner,
        ExpenseRecordDto expense,
        string requestElement,
        string idElement
    )
    {
        return $"<doc:{requestElement}>"
            + $"<pincode>{pinCode}</pincode>"
            + BuildProprietarioXml(owner)
            + $"<{idElement}>{BuildDocumentoSpesaXml(expense)}</{idElement}>"
            + $"</doc:{requestElement}>";
    }

    private static string BuildProprietarioXml(OwnerDto? owner)
    {
        if (owner is null)
            return "";
        var xml = "<Proprietario>";
        if (!string.IsNullOrEmpty(owner.CodiceRegione))
            xml += $"<codiceRegione>{owner.CodiceRegione}</codiceRegione>";
        if (!string.IsNullOrEmpty(owner.CodiceAsl))
            xml += $"<codiceAsl>{owner.CodiceAsl}</codiceAsl>";
        if (!string.IsNullOrEmpty(owner.CodiceSsa))
            xml += $"<codiceSSA>{owner.CodiceSsa}</codiceSSA>";
        if (!string.IsNullOrEmpty(owner.CfProprietario))
            xml += $"<cfProprietario>{owner.CfProprietario}</cfProprietario>";
        xml += "</Proprietario>";
        return xml;
    }

    private static string BuildDocumentoSpesaXml(ExpenseRecordDto expense)
    {
        var xml =
            $"<idSpesa>"
            + $"<pIva>{expense.PIva}</pIva>"
            + $"<dataEmissione>{expense.DataEmissione:yyyy-MM-dd}</dataEmissione>"
            + $"<numDocumentoFiscale><dispositivo>{expense.Dispositivo}</dispositivo><numDocumento>{expense.NumDocumento}</numDocumento></numDocumentoFiscale>"
            + $"</idSpesa>"
            + $"<dataPagamento>{expense.DataPagamento:yyyy-MM-dd}</dataPagamento>";

        if (expense.FlagPagamentoAnticipato.HasValue)
            xml +=
                $"<flagPagamentoAnticipato>{expense.FlagPagamentoAnticipato.Value}</flagPagamentoAnticipato>";

        if (!string.IsNullOrEmpty(expense.CfCittadino))
            xml += $"<cfCittadino>{expense.CfCittadino}</cfCittadino>";

        foreach (var item in expense.Items ?? new List<ExpenseItemDto>())
        {
            xml += $"<voceSpesa><tipoSpesa>{item.TipoSpesa}</tipoSpesa>";
            if (!string.IsNullOrEmpty(item.FlagTipoSpesa))
                xml += $"<flagTipoSpesa>{item.FlagTipoSpesa}</flagTipoSpesa>";
            xml += $"<importo>{item.Importo:F2}</importo>";
            if (item.AliquotaIva.HasValue)
                xml += $"<aliquotaIVA>{item.AliquotaIva.Value:F2}</aliquotaIVA>";
            else if (!string.IsNullOrEmpty(item.NaturaIva))
                xml += $"<naturaIVA>{item.NaturaIva}</naturaIVA>";
            xml += "</voceSpesa>";
        }

        if (!string.IsNullOrEmpty(expense.PagamentoTracciato))
            xml += $"<pagamentoTracciato>{expense.PagamentoTracciato}</pagamentoTracciato>";
        if (!string.IsNullOrEmpty(expense.TipoDocumento))
            xml += $"<tipoDocumento>{expense.TipoDocumento}</tipoDocumento>";
        if (!string.IsNullOrEmpty(expense.FlagOpposizione))
            xml += $"<flagOpposizione>{expense.FlagOpposizione}</flagOpposizione>";

        return xml;
    }

    private static SincronoResultDto ParseSyncResponse(string xml, string responseElement)
    {
        var doc = XDocument.Parse(xml);
        var responseEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == responseElement);
        if (responseEl is null)
            return new SincronoResultDto
            {
                Success = false,
                Errors = new[] { $"No {responseElement} in response" },
            };

        var esitoChiamata = GetElementValue(responseEl, "esitoChiamata") ?? "";
        var protocollo = GetElementValue(responseEl, "protocollo");
        var success = esitoChiamata == "0" || esitoChiamata == "000";

        var messaggi = ParseMessaggi(GetElement(responseEl, "listaMessaggi"));

        return new SincronoResultDto
        {
            Success = success,
            EsitoChiamata = esitoChiamata,
            Protocollo = protocollo,
            Messaggi = messaggi,
            Errors = success
                ? Array.Empty<string>()
                : messaggi
                    .Where(m => m.Tipo == "E")
                    .Select(m => $"[{m.Codice}] {m.Descrizione}")
                    .ToList(),
        };
    }
}
