using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;

namespace Swisschain.Sdk.Server.Logging
{
    public static class LogConfigurator
    {
        public static ILoggerFactory Configure(string productName = default,
            IReadOnlyCollection<string> remoteSettingsUrls = default, Func<IConfigurationRoot, IReadOnlyDictionary<string, string>> additionalPropertiesFactory = default)
        {
            Console.WriteLine($"App - name: {ApplicationInformation.AppName}");
            Console.WriteLine($"App - version: {ApplicationInformation.AppVersion}");

            IConfigurationRoot configRoot = BuildConfigRoot(remoteSettingsUrls);

            var config = new LoggerConfiguration()
                .ReadFrom.Configuration(configRoot)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionData()
                .Enrich.WithCorrelationIdHeader()
                .Enrich.WithRequestIdHeader();

            SetupProperty(productName, config, configRoot, additionalPropertiesFactory);

            SetupConsole(configRoot, config);

            SetupSeq(configRoot, config);

            SetupElasticsearch(configRoot, config);

            Log.Logger = config.CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal((Exception)e.ExceptionObject, "Application has been terminated unexpectedly");
                Log.CloseAndFlush();
            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Log.CloseAndFlush();
            };

            return new LoggerFactory().AddSerilog();
        }

        private static IConfigurationRoot BuildConfigRoot(IReadOnlyCollection<string> remoteSettingsUrls)
        {
            var configBuilder = new ConfigurationBuilder();

            var isRemoteSettingsRequired = ApplicationEnvironment.Config.GetValue("RemoteSettingsRequired", false);

            Console.WriteLine($"Env - RemoteSettingsRequired: {isRemoteSettingsRequired}");

            if (remoteSettingsUrls != null)
            {
                foreach (var remoteSettingsUrl in remoteSettingsUrls)
                {
                    configBuilder.AddWebJsonConfiguration(WebJsonHttpClientProvider.DefaultClient, remoteSettingsUrl,
                        isOptional: !isRemoteSettingsRequired);
                }
            }

            configBuilder
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{ApplicationEnvironment.Environment}.json", optional: true)
                .AddEnvironmentVariables();

            var configRoot = configBuilder.Build();
            return configRoot;
        }

        private static void SetupProperty(string productName, LoggerConfiguration config,
            IConfigurationRoot configRoot,
            Func<IConfigurationRoot, IReadOnlyDictionary<string, string>> additionalPropertiesFactory)
        {
            var properties = additionalPropertiesFactory?.Invoke(configRoot)
                             ?? new Dictionary<string, string>();

            config
                .Enrich.WithProperty("app-name", properties.ContainsKey("app-name") ? properties["app-name"] : ApplicationInformation.AppName)
                .Enrich.WithProperty("app-version", ApplicationInformation.AppVersion)
                .Enrich.WithProperty("host-name", ApplicationEnvironment.HostName ?? ApplicationEnvironment.UserName)
                .Enrich.WithProperty("environment", ApplicationEnvironment.Environment)
                .Enrich.WithProperty("started-at", ApplicationInformation.StartedAt);

            if (productName != default)
            {
                config.Enrich.WithProperty("product-name", productName);
            }

            foreach (var (name, value) in properties)
            {
                config.Enrich.WithProperty(name, value);
            }
        }

        private static void SetupConsole(IConfigurationRoot configRoot, LoggerConfiguration config)
        {
            var logLevel = configRoot["ConsoleOutputLogLevel"];

            if (!string.IsNullOrEmpty(logLevel) && Enum.TryParse<LogEventLevel>(logLevel, out var restrictedToMinimumLevel))
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Env - ConsoleOutputLogLevel: {restrictedToMinimumLevel}");
                Console.ForegroundColor = color;

                config.WriteTo.Console(restrictedToMinimumLevel);
            }
            else
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Env - ConsoleOutputLogLevel: <not specified> (default)");
                Console.ForegroundColor = color;

                config.WriteTo.Console();
            }
        }

        private static void SetupSeq(IConfigurationRoot configRoot, LoggerConfiguration config)
        {
            var seqUrl = configRoot["SeqUrl"];

            if (seqUrl != default)
            {
                config.WriteTo.Seq(seqUrl, period: TimeSpan.FromSeconds(1));
            }
        }

        private static void SetupElasticsearch(IConfigurationRoot configRoot, LoggerConfiguration config)
        {
            var elasticsearchUrlsConfig = configRoot.Get<ElasticsearchConfig>()?.ElasticsearchLogs;


            if (elasticsearchUrlsConfig?.NodeUrls != null && elasticsearchUrlsConfig.NodeUrls.Any())
            {
                var indexPrefix = elasticsearchUrlsConfig?.IndexPrefixName ?? "log";

                config.WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(elasticsearchUrlsConfig.NodeUrls.Select(u => new Uri(u)))
                    {
                        AutoRegisterTemplate = true,
                        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                        IndexDecider = (e, o) => $"{indexPrefix}-{o.Date:yyyy-MM-dd}",
                    });

                Console.WriteLine($"Setup logging to Elasticsearch. Index name: {indexPrefix}-yyyy-MM-dd");
            }
        }

        private sealed class ElasticsearchUrlsConfig
        {
            public IReadOnlyCollection<string> NodeUrls { get; set; }
            public string IndexPrefixName { get; set; }
        }

        private sealed class ElasticsearchConfig
        {
            public ElasticsearchUrlsConfig ElasticsearchLogs { get; set; }
        }

    }
}
