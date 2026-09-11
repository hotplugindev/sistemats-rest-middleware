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

        var owner = request.Owner;
        if (owner is not null && !string.IsNullOrEmpty(owner.CfProprietario))
        {
            owner = new OwnerDto
            {
                CodiceRegione = owner.CodiceRegione,
                CodiceAsl = owner.CodiceAsl,
                CodiceSsa = owner.CodiceSsa,
                CfProprietario = _cryptoService.EncryptToBase64(owner.CfProprietario)
            };
        }

        var xml = _xmlGenerator.GenerateXml(request);

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
            Owner = owner,
            ZipContent = zipContent,
            Credentials = request.Credentials
        };

        return await _soapClient.InviaFileAsync(soapRequest, cancellationToken);
    }
}
