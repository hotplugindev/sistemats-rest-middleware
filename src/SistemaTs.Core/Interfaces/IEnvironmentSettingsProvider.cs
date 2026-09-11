namespace SistemaTs.Core.Interfaces;

public interface IEnvironmentSettingsProvider
{
    bool IsProduction { get; }
    string InvioEndpointUrl { get; }
    string EsitoInviiEndpointUrl { get; }
    string DettaglioErroriEndpointUrl { get; }
    string RicevutaPdfEndpointUrl { get; }
    string InterrogazionePuntualeEndpointUrl { get; }
    string ReportMensileEndpointUrl { get; }
    string DocumentoSpesaEndpointUrl { get; }
    string DettaglioSegnalazioneEndpointUrl { get; }
    string ReportSegnalazioniEndpointUrl { get; }
}
