using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class EnvironmentSettingsProviderTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("Yes", true)]
    [InlineData("YES", true)]
    [InlineData(" true ", true)]
    [InlineData(" 1 ", true)]
    public void ParseIsProduction_TruthyValues_ReturnsTrue(string? value, bool expected)
    {
        Assert.Equal(expected, EnvironmentSettingsProvider.ParseIsProduction(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("0")]
    [InlineData("no")]
    [InlineData("anything")]
    public void ParseIsProduction_FalsyValues_ReturnsFalse(string? value)
    {
        Assert.False(EnvironmentSettingsProvider.ParseIsProduction(value));
    }

    [Fact]
    public void Default_UsesTestEndpoints()
    {
        var sut = new EnvironmentSettingsProvider(null);

        Assert.False(sut.IsProduction);
        Assert.Contains("invioSS730pTest", sut.InvioEndpointUrl);
        Assert.Contains("invioSS730pTest", sut.EsitoInviiEndpointUrl);
        Assert.Contains("invioSS730pTest", sut.DettaglioErroriEndpointUrl);
        Assert.Contains("invioSS730pTest", sut.RicevutaPdfEndpointUrl);
        Assert.Contains("invioSS730pTest", sut.InterrogazionePuntualeEndpointUrl);
        Assert.Contains("invioSS730pTest", sut.ReportMensileEndpointUrl);
        Assert.Contains("invioSS730pTest", sut.DocumentoSpesaEndpointUrl);
        Assert.Contains("invioSS730pTest", sut.DettaglioSegnalazioneEndpointUrl);
        Assert.Contains("invioSS730pTest", sut.ReportSegnalazioniEndpointUrl);
    }

    [Fact]
    public void Production_UsesProductionEndpoints()
    {
        var sut = new EnvironmentSettingsProvider("true");

        Assert.True(sut.IsProduction);
        Assert.Contains("invioSS730p.sanita", sut.InvioEndpointUrl);
        Assert.DoesNotContain("Test", sut.InvioEndpointUrl);
    }

    [Fact]
    public void Production_InvioEndpointUrl_IsCorrect()
    {
        var sut = new EnvironmentSettingsProvider("true");
        Assert.Equal("https://invioSS730p.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort", sut.InvioEndpointUrl);
    }

    [Fact]
    public void Test_InvioEndpointUrl_IsCorrect()
    {
        var sut = new EnvironmentSettingsProvider(null);
        Assert.Equal("https://invioSS730pTest.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort", sut.InvioEndpointUrl);
    }

    [Fact]
    public void Production_EsitoInviiEndpointUrl_IsCorrect()
    {
        var sut = new EnvironmentSettingsProvider("1");
        Assert.Equal("https://invioSS730p.sanita.finanze.it/EsitoStatoInviiWEB/EsitoInvioDatiSpesa730Service", sut.EsitoInviiEndpointUrl);
    }

    [Fact]
    public void Production_DocumentoSpesaEndpointUrl_IsCorrect()
    {
        var sut = new EnvironmentSettingsProvider("yes");
        Assert.Equal("https://invioSS730p.sanita.finanze.it/DocumentoSpesa730pWeb/DocumentoSpesa730pPort", sut.DocumentoSpesaEndpointUrl);
    }
}
