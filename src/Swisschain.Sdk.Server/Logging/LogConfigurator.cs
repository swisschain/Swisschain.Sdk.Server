using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;

namespace Swisschain.Sdk.Server.Logging
{
    public static class LogConfigurator
    {
        public static ILoggerFactory Configure(string productName = default,
            IReadOnlyCollection<string> remoteSettingsUrls = default)
        {
            var configBuilder = new ConfigurationBuilder();
                
            if (remoteSettingsUrls != null)
            {
                foreach (var remoteSettingsUrl in remoteSettingsUrls)
                {
                    configBuilder.AddWebJsonConfiguration(WebJsonHttpClientProvider.DefaultClient, remoteSettingsUrl, isOptional: true);
                }
            }

            configBuilder
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{ApplicationEnvironment.Environment}.json", optional: true)
                .AddEnvironmentVariables();

            var configRoot = configBuilder.Build();

            var config = new LoggerConfiguration()
                .ReadFrom.Configuration(configRoot)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionData()
                .Enrich.WithCorrelationIdHeader()
                .Enrich.WithProperty("app-version", ApplicationInformation.AppVersion)
                .Enrich.WithProperty("host-name", ApplicationEnvironment.HostName ?? ApplicationEnvironment.UserName)
                .Enrich.WithProperty("environment", ApplicationEnvironment.Environment)
                .Enrich.WithProperty("started-at", ApplicationInformation.StartedAt)
                .WriteTo.Console();

            if (productName != default)
            {
                config.Enrich.WithProperty("product-name", productName);

                var appName = string.Join('.', ApplicationInformation.AppName
                    .Split('.', StringSplitOptions.RemoveEmptyEntries)
                    .SkipWhile(x => x != productName)
                    .Skip(1)
                    .ToArray());

                if (string.IsNullOrEmpty(appName))
                {
                    appName = ApplicationInformation.AppName;
                }

                config.Enrich.WithProperty("app-name", appName);

            }
            else
            {
                config.Enrich.WithProperty("app-name", ApplicationInformation.AppName);
            }

            var seqUrl = configRoot["SeqUrl"];

            if (seqUrl != default)
            {
                config.WriteTo.Seq(seqUrl, period: TimeSpan.FromSeconds(1));
            }

            Log.Logger = config.CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal((Exception)e.ExceptionObject, "Application has been terminated unexpectedly");
                Log.CloseAndFlush();
            };

            return new LoggerFactory().AddSerilog();
        }
    }
}