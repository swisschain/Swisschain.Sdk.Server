using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Configuration;

namespace Swisschain.Sdk.Server.Common
{
    public static class HostBuilderTemplate
    {
        public static IHostBuilder SwisschainService<TStartup>(this IHostBuilder host,
            Action<HostOptionsBuilder> optionsBuilderConfigurator) where TStartup : class
        {
            return SwisschainService<TStartup>(host,
                optionsBuilderConfigurator,
                builder => { }, 
                (context, builder) => { });
        }

        public static IHostBuilder SwisschainService<TStartup>(this IHostBuilder host,
            Action<HostOptionsBuilder> optionsBuilderConfigurator,
            Action<IWebHostBuilder> optionsWebHostBuilder,
            Action<HostBuilderContext, IConfigurationBuilder> optionsConfigurationBuilder)

            where TStartup : class
        {
            var optionsBuilder = new HostOptionsBuilder();

            if (ApplicationEnvironment.Config["HttpPort"] != default)
            {
                optionsBuilder.HttpPort = int.Parse(ApplicationEnvironment.Config["HttpPort"]);
            }

            if (ApplicationEnvironment.Config["GrpcPort"] != default)
            {
                optionsBuilder.GrpcPort = int.Parse(ApplicationEnvironment.Config["GrpcPort"]);
            }

            optionsBuilderConfigurator.Invoke(optionsBuilder);

            return host
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConsoleLifetime()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<TStartup>();
                    webBuilder.ConfigureKestrel(options =>
                    {
                        Console.WriteLine($"Options - HttpPort: {optionsBuilder.HttpPort}");
                        Console.WriteLine($"Options - GrpcPort: {optionsBuilder.GrpcPort}");

                        options.Listen(IPAddress.Any, optionsBuilder.HttpPort, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });

                        options.Listen(IPAddress.Any, optionsBuilder.GrpcPort, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    });
                    optionsWebHostBuilder(webBuilder);
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.Sources.Clear();
                    config.AddSwisschainConfiguration(optionsBuilder.WebJsonConfigurationSourcesBuilder,
                        optionsBuilder.FileJsonSettingsLocations);

                    optionsConfigurationBuilder(hostingContext, config);

                    config.Build().ValidateSubstitutions();
                })
                .ConfigureServices(services =>
                {
                    if (optionsBuilder.LoggerFactory != null)
                    {
                        services.AddSingleton(optionsBuilder.LoggerFactory);
                        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    }
                });
        }
    }
}
