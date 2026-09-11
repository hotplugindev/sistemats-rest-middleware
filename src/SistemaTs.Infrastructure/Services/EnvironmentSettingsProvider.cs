using SistemaTs.Core.Interfaces;

namespace SistemaTs.Infrastructure.Services;

public sealed class EnvironmentSettingsProvider : IEnvironmentSettingsProvider
{
    public const string ProductionEnvironmentVariable = "PRODUCTION_ENVIRONMENT";

    private const string TestHost = "invioSS730pTest.sanita.finanze.it";
    private const string ProductionHost = "invioSS730p.sanita.finanze.it";

    public EnvironmentSettingsProvider()
        : this(Environment.GetEnvironmentVariable(ProductionEnvironmentVariable))
    {
    }

    public EnvironmentSettingsProvider(string? productionEnvironmentValue)
    {
        IsProduction = ParseIsProduction(productionEnvironmentValue);
    }

    public bool IsProduction { get; }

    private string Host => IsProduction ? ProductionHost : TestHost;

    public string InvioEndpointUrl =>
        $"https://{Host}/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort";

    public string EsitoInviiEndpointUrl =>
        $"https://{Host}/EsitoStatoInviiWEB/EsitoInvioDatiSpesa730Service";

    public string DettaglioErroriEndpointUrl =>
        $"https://{Host}/EsitoStatoInviiWEB/DettaglioErrori730Service";

    public string RicevutaPdfEndpointUrl =>
        $"https://{Host}/Ricevute730ServiceWeb/ricevutePdf";

    public string InterrogazionePuntualeEndpointUrl =>
        $"https://{Host}/InterrogazionePuntuale730Web/InterrogazionePuntuale730Port";

    public string ReportMensileEndpointUrl =>
        $"https://{Host}/ReportMensile730Web/ReportMensilePort";

    public string DocumentoSpesaEndpointUrl =>
        $"https://{Host}/DocumentoSpesa730pWeb/DocumentoSpesa730pPort";

    public string DettaglioSegnalazioneEndpointUrl =>
        $"https://{Host}/Interrogazioni730pWeb/DettaglioSegnalazione730pPort";

    public string ReportSegnalazioniEndpointUrl =>
        $"https://{Host}/Interrogazioni730pWeb/ReportSegnalazioni730pPort";

    internal static bool ParseIsProduction(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
