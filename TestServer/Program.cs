using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Logging;

namespace TestServer
{
    class Program
    {
        private sealed class RemoteSettingsUrlsConfig
        {
            public IReadOnlyCollection<string> RemoteSettingsUrls { get; set; }
        }

        public static void Main(string[] args)
        {
            var remoteSettingsUrlsConfig = ApplicationEnvironment.Config.Get<RemoteSettingsUrlsConfig>();

            using var loggerFactory = LogConfigurator.Configure(
                "Sdk",
                remoteSettingsUrlsConfig.RemoteSettingsUrls ?? Array.Empty<string>());

            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                logger.LogInformation("Application is being started");

                CreateHostBuilder(loggerFactory, remoteSettingsUrlsConfig).Build().Run();

                logger.LogInformation("Application has been stopped");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Application has been terminated unexpectedly");
            }
        }

        private static IHostBuilder CreateHostBuilder(ILoggerFactory loggerFactory, RemoteSettingsUrlsConfig remoteSettingsUrlsConfig) =>
            new HostBuilder()
                .SwisschainService<Startup>(options =>
                {
                    options.UseLoggerFactory(loggerFactory);
                    options.AddWebJsonConfigurationSources(remoteSettingsUrlsConfig.RemoteSettingsUrls ?? Array.Empty<string>());
                });
    }
}
