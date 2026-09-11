using System.Net.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaTs.Core.Interfaces;
using SistemaTs.Infrastructure.Configuration;
using SistemaTs.Infrastructure.Services;

namespace SistemaTs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSistemaTsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<SistemaTsOptions>(
            configuration.GetSection(SistemaTsOptions.SectionName)
        );

        services.AddSingleton<IXmlGeneratorService, XmlGeneratorService>();
        services.AddSingleton<IXmlValidationService, XmlValidationService>();
        services.AddSingleton<ICryptoService, SanitelCryptoService>();
        services.AddSingleton<IZipService, ZipCompressionService>();

        services
            .AddHttpClient<ISistemaTsClient, SistemaTsSoapClient>()
            .ConfigurePrimaryHttpMessageHandler(CreateHandler);
        services
            .AddHttpClient<IRicevuteClient, RicevuteSoapClient>()
            .ConfigurePrimaryHttpMessageHandler(CreateHandler);
        services
            .AddHttpClient<IInterrogazioniClient, InterrogazioniClient>()
            .ConfigurePrimaryHttpMessageHandler(CreateHandler);
        services
            .AddHttpClient<IDocumentoSpesaClient, DocumentoSpesaSoapClient>()
            .ConfigurePrimaryHttpMessageHandler(CreateHandler);
        services
            .AddHttpClient<ISegnalazioniClient, SegnalazioniSoapClient>()
            .ConfigurePrimaryHttpMessageHandler(CreateHandler);

        services.AddScoped<IExpenseService, ExpenseService>();

        return services;
    }

    private static SocketsHttpHandler CreateHandler() =>
        new()
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (
                    sender,
                    certificate,
                    chain,
                    sslPolicyErrors
                ) => true,
            },
        };
}
