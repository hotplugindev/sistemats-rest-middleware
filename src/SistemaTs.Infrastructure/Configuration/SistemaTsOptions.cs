namespace SistemaTs.Infrastructure.Configuration;

public sealed class SistemaTsOptions
{
    public const string SectionName = "SistemaTs";

    public string InvioEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort";

    public string EsitoInviiEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/EsitoStatoInviiWEB/EsitoInvioDatiSpesa730Service";

    public string DettaglioErroriEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/EsitoStatoInviiWEB/DettaglioErrori730Service";

    public string RicevutaPdfEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/Ricevute730ServiceWeb/ricevutePdf";

    public string InterrogazionePuntualeEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/InterrogazionePuntuale730Web/InterrogazionePuntuale730Port";

    public string ReportMensileEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/ReportMensile730Web/ReportMensilePort";

    public string DocumentoSpesaEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/DocumentoSpesa730pWeb/DocumentoSpesa730pPort";

    public string DettaglioSegnalazioneEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/Interrogazioni730pWeb/DettaglioSegnalazione730pPort";

    public string ReportSegnalazioniEndpointUrl { get; set; } =
        "https://invioSS730pTest.sanita.finanze.it/Interrogazioni730pWeb/ReportSegnalazioni730pPort";

    public string CertificatePath { get; set; } = "SanitelCF.cer";

    public string XsdSchemaPath { get; set; } = "730_precompilata.xsd";
}
