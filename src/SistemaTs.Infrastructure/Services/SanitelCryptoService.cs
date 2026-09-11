using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Configuration;

namespace SistemaTs.Infrastructure.Services;

public sealed class SanitelCryptoService : ICryptoService
{
    private readonly RSA _publicKey;

    public SanitelCryptoService(IOptions<SistemaTsOptions> options)
    {
        var certPath = ResolvePath(options.Value.CertificatePath);
        var cert = X509CertificateLoader.LoadCertificateFromFile(certPath);
        _publicKey = cert.GetRSAPublicKey()
            ?? throw new InvalidOperationException("Certificate does not contain an RSA public key.");
    }

    internal static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path) && File.Exists(path))
            return path;
        var candidate = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(candidate))
            return candidate;
        if (File.Exists(path))
            return path;
        throw new FileNotFoundException($"Required file not found: {path}", path);
    }

    public string EncryptToBase64(string plainText)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(plainText);
        var encrypted = _publicKey.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        return Convert.ToBase64String(encrypted);
    }
}
