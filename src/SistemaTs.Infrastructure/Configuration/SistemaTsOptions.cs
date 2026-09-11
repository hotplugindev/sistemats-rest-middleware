namespace SistemaTs.Infrastructure.Configuration;

public sealed class SistemaTsOptions
{
    public const string SectionName = "SistemaTs";

    public string EndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort";

    public string CertificatePath { get; set; } = "SanitelCF.cer";

    public string XsdSchemaPath { get; set; } = "730_precompilata.xsd";
}
