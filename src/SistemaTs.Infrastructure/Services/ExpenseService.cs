using SistemaTs.Core.Dtos;
using SistemaTs.Core.Interfaces;

namespace SistemaTs.Infrastructure.Services;

public sealed class ExpenseService : IExpenseService
{
    private readonly IXmlGeneratorService _xmlGenerator;
    private readonly IXmlValidationService _xmlValidator;
    private readonly ICryptoService _cryptoService;
    private readonly IZipService _zipService;
    private readonly ISistemaTsClient _soapClient;

    public ExpenseService(
        IXmlGeneratorService xmlGenerator,
        IXmlValidationService xmlValidator,
        ICryptoService cryptoService,
        IZipService zipService,
        ISistemaTsClient soapClient)
    {
        _xmlGenerator = xmlGenerator;
        _xmlValidator = xmlValidator;
        _cryptoService = cryptoService;
        _zipService = zipService;
        _soapClient = soapClient;
    }

    public async Task<SubmissionResultDto> SubmitAsync(ExpenseSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var encryptedPincode = _cryptoService.EncryptToBase64(request.Credentials!.Pincode!);

        var xmlRequest = BuildXmlRequest(request);
        var xml = _xmlGenerator.GenerateXml(xmlRequest);

        var validationResult = _xmlValidator.Validate(xml);
        if (!validationResult.IsValid)
        {
            return SubmissionResultDto.Failure(validationResult.Errors);
        }

        var entryName = request.XmlEntryName ?? "730_precompilata.xml";
        var zipContent = _zipService.CompressToZip(xml, entryName);

        var attachmentName = request.AttachmentName ?? "invio730.zip";

        var soapRequest = new SoapSubmissionRequest
        {
            NomeFileAllegato = attachmentName,
            PincodeInvianteCifrato = encryptedPincode,
            Owner = request.Owner,
            ZipContent = zipContent,
            Credentials = request.Credentials
        };

        return await _soapClient.InviaFileAsync(soapRequest, cancellationToken);
    }

    private ExpenseSubmissionRequest BuildXmlRequest(ExpenseSubmissionRequest request)
    {
        OwnerDto? xmlOwner = null;
        if (request.Owner is not null)
        {
            xmlOwner = new OwnerDto
            {
                CodiceRegione = request.Owner.CodiceRegione,
                CodiceAsl = request.Owner.CodiceAsl,
                CodiceSsa = request.Owner.CodiceSsa,
                CfProprietario = !string.IsNullOrEmpty(request.Owner.CfProprietario)
                    ? _cryptoService.EncryptToBase64(request.Owner.CfProprietario)
                    : null
            };
        }

        var xmlExpenses = request.Expenses!.Select(e => new ExpenseRecordDto
        {
            PIva = e.PIva,
            DataEmissione = e.DataEmissione,
            Dispositivo = e.Dispositivo,
            NumDocumento = e.NumDocumento,
            DataPagamento = e.DataPagamento,
            FlagPagamentoAnticipato = e.FlagPagamentoAnticipato,
            FlagOperazione = e.FlagOperazione,
            CfCittadino = !string.IsNullOrEmpty(e.CfCittadino)
                ? _cryptoService.EncryptToBase64(e.CfCittadino)
                : null,
            PagamentoTracciato = e.PagamentoTracciato,
            TipoDocumento = e.TipoDocumento,
            FlagOpposizione = e.FlagOpposizione,
            Items = e.Items
        }).ToList();

        return new ExpenseSubmissionRequest
        {
            Credentials = request.Credentials,
            Owner = xmlOwner,
            AttachmentName = request.AttachmentName,
            XmlEntryName = request.XmlEntryName,
            Expenses = xmlExpenses
        };
    }
}
