using System;
using System.IO;
using System.Net;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;

namespace Swisschain.Sdk.Server.Common
{
    public static class HostBuilderTemplate
    {
        public static IHostBuilder SwisschainService<TStartup>(this IHostBuilder host, Action<HostOptionsBuilder> optionsBuilderConfigurator)

            where TStartup : class
        {
            var optionsBuilder = new HostOptionsBuilder();

            optionsBuilderConfigurator.Invoke(optionsBuilder);

            return host
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureWebHostDefaults(options => { })
                .UseConsoleLifetime()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.Sources.Clear();

                    if (optionsBuilder.WebJsonConfigurationSourceBuilder != null)
                    {
                        config.AddWebJsonConfiguration(optionsBuilder.WebJsonConfigurationSourceBuilder);
                    }

                    // TODO: AddAzureBlobConfiguration()
                    // TODO: AddSecretsManagerConfiguration

                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices(services =>
                {
                    if (optionsBuilder.LoggerFactory != null)
                    {
                        services.AddSingleton(optionsBuilder.LoggerFactory);
                        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<TStartup>();

                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, optionsBuilder.RestPort, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });

                        options.Listen(IPAddress.Any, optionsBuilder.GrpcPort, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    });
                });
        }
    }
}