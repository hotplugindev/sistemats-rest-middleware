using Microsoft.Extensions.Options;
using SistemaTs.Infrastructure.Configuration;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.UnitTests;

public class CryptoTests
{
    private static string GetCertPath()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "SanitelCF.cer");
        if (File.Exists(candidate))
            return candidate;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SanitelCF.cer")))
            dir = dir.Parent;
        if (dir is null)
            throw new FileNotFoundException("SanitelCF.cer not found");
        return Path.Combine(dir.FullName, "SanitelCF.cer");
    }

    [Fact]
    public void EncryptToBase64_ReturnsValidBase64()
    {
        var certPath = GetCertPath();
        if (!File.Exists(certPath))
            throw new FileNotFoundException($"Certificate not found at {certPath}");

        var options = Options.Create(new SistemaTsOptions { CertificatePath = certPath });
        var sut = new SanitelCryptoService(options);

        var result = sut.EncryptToBase64("1234567890");

        Assert.False(string.IsNullOrEmpty(result));
        var bytes = Convert.FromBase64String(result);
        Assert.Equal(128, bytes.Length);
    }

    [Fact]
    public void EncryptToBase64_DifferentInputs_ProduceDifferentOutputs()
    {
        var certPath = GetCertPath();
        if (!File.Exists(certPath))
            throw new FileNotFoundException($"Certificate not found at {certPath}");

        var options = Options.Create(new SistemaTsOptions { CertificatePath = certPath });
        var sut = new SanitelCryptoService(options);

        var result1 = sut.EncryptToBase64("1234567890");
        var result2 = sut.EncryptToBase64("0987654321");

        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void EncryptToBase64_SameInput_ProducesDifferentOutputs_DueToPadding()
    {
        var certPath = GetCertPath();
        if (!File.Exists(certPath))
            throw new FileNotFoundException($"Certificate not found at {certPath}");

        var options = Options.Create(new SistemaTsOptions { CertificatePath = certPath });
        var sut = new SanitelCryptoService(options);

        var result1 = sut.EncryptToBase64("test");
        var result2 = sut.EncryptToBase64("test");

        Assert.NotEqual(result1, result2);
    }
}
