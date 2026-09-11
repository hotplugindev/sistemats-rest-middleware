using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Configuration;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSistemaTsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SistemaTsOptions>(configuration.GetSection(SistemaTsOptions.SectionName));

        services.AddSingleton<IXmlGeneratorService, XmlGeneratorService>();
        services.AddSingleton<IXmlValidationService, XmlValidationService>();
        services.AddSingleton<ICryptoService, SanitelCryptoService>();
        services.AddSingleton<IZipService, ZipCompressionService>();
        services.AddHttpClient<ISistemaTsClient, SistemaTsSoapClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                }
            });
        services.AddScoped<IExpenseService, ExpenseService>();

        return services;
    }
}
