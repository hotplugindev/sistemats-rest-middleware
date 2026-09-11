using System.Globalization;
using System.Xml.Linq;
using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Infrastructure.Services;

public sealed class XmlGeneratorService : IXmlGeneratorService
{
    public string GenerateXml(ExpenseSubmissionRequest request)
    {
        var root = new XElement("precompilata",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute(XNamespace.Xmlns + "noNamespaceSchemaLocation", "730_precompilata.xsd"));

        if (request.Owner is not null)
        {
            var proprietario = new XElement("proprietario");
            if (!string.IsNullOrEmpty(request.Owner.CodiceRegione))
                proprietario.Add(new XElement("codiceRegione", request.Owner.CodiceRegione));
            if (!string.IsNullOrEmpty(request.Owner.CodiceAsl))
                proprietario.Add(new XElement("codiceAsl", request.Owner.CodiceAsl));
            if (!string.IsNullOrEmpty(request.Owner.CodiceSsa))
                proprietario.Add(new XElement("codiceSSA", request.Owner.CodiceSsa));
            if (!string.IsNullOrEmpty(request.Owner.CfProprietario))
                proprietario.Add(new XElement("cfProprietario", request.Owner.CfProprietario));
            root.Add(proprietario);
        }
        else
        {
            root.Add(new XElement("proprietario"));
        }

        foreach (var expense in request.Expenses!)
        {
            root.Add(BuildDocumentoSpesa(expense));
        }

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    private static XElement BuildDocumentoSpesa(ExpenseRecordDto expense)
    {
        var doc = new XElement("documentoSpesa");

        var idSpesa = new XElement("idSpesa",
            new XElement("pIva", expense.PIva),
            new XElement("dataEmissione", expense.DataEmissione.ToString("yyyy-MM-dd")),
            new XElement("numDocumentoFiscale",
                new XElement("dispositivo", expense.Dispositivo.ToString(CultureInfo.InvariantCulture)),
                new XElement("numDocumento", expense.NumDocumento)));
        doc.Add(idSpesa);

        doc.Add(new XElement("dataPagamento", expense.DataPagamento.ToString("yyyy-MM-dd")));

        if (expense.FlagPagamentoAnticipato.HasValue)
            doc.Add(new XElement("flagPagamentoAnticipato", expense.FlagPagamentoAnticipato.Value.ToString(CultureInfo.InvariantCulture)));

        doc.Add(new XElement("flagOperazione", expense.FlagOperazione));

        if (!string.IsNullOrEmpty(expense.CfCittadino))
            doc.Add(new XElement("cfCittadino", expense.CfCittadino));

        if (!string.IsNullOrEmpty(expense.PagamentoTracciato))
            doc.Add(new XElement("pagamentoTracciato", expense.PagamentoTracciato));

        if (!string.IsNullOrEmpty(expense.TipoDocumento))
            doc.Add(new XElement("tipoDocumento", expense.TipoDocumento));

        if (!string.IsNullOrEmpty(expense.FlagOpposizione))
            doc.Add(new XElement("flagOpposizione", expense.FlagOpposizione));

        foreach (var item in expense.Items!)
        {
            var voce = new XElement("voceSpesa",
                new XElement("tipoSpesa", item.TipoSpesa));

            if (!string.IsNullOrEmpty(item.FlagTipoSpesa))
                voce.Add(new XElement("flagTipoSpesa", item.FlagTipoSpesa));

            voce.Add(new XElement("importo", item.Importo.ToString("0.00", CultureInfo.InvariantCulture)));

            if (item.AliquotaIva.HasValue)
                voce.Add(new XElement("aliquotaIVA", item.AliquotaIva.Value.ToString("0.00", CultureInfo.InvariantCulture)));
            else if (!string.IsNullOrEmpty(item.NaturaIva))
                voce.Add(new XElement("naturaIVA", item.NaturaIva));

            doc.Add(voce);
        }

        return doc;
    }
}
