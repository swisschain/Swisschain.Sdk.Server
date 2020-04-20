using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;

namespace Swisschain.Sdk.Server.Logging
{
    public static class LogConfigurator
    {
        public static ILoggerFactory Configure(string productName = default,
            IReadOnlyCollection<string> remoteSettingsUrls = default)
        {
            IConfigurationRoot configRoot = BuildConfigRoot(remoteSettingsUrls);

            var config = new LoggerConfiguration()
                .ReadFrom.Configuration(configRoot)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionData()
                .Enrich.WithCorrelationIdHeader();

            SetupProperty(productName, config);

            SetupConsole(configRoot, config);

            SetupSeq(configRoot, config);

            SetupElasticsearch(configRoot, config);

            Log.Logger = config.CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal((Exception)e.ExceptionObject, "Application has been terminated unexpectedly");
                Log.CloseAndFlush();
            };

            return new LoggerFactory().AddSerilog();
        }

        private static IConfigurationRoot BuildConfigRoot(IReadOnlyCollection<string> remoteSettingsUrls)
        {
            var configBuilder = new ConfigurationBuilder();

            if (remoteSettingsUrls != null)
            {
                foreach (var remoteSettingsUrl in remoteSettingsUrls)
                {
                    configBuilder.AddWebJsonConfiguration(WebJsonHttpClientProvider.DefaultClient, remoteSettingsUrl,
                        isOptional: true);
                }
            }

            configBuilder
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{ApplicationEnvironment.Environment}.json", optional: true)
                .AddEnvironmentVariables();

            var configRoot = configBuilder.Build();
            return configRoot;
        }

        private static void SetupProperty(string productName, LoggerConfiguration config)
        {
            config
                .Enrich.WithProperty("app-name", ApplicationInformation.AppName)
                .Enrich.WithProperty("app-version", ApplicationInformation.AppVersion)
                .Enrich.WithProperty("host-name", ApplicationEnvironment.HostName ?? ApplicationEnvironment.UserName)
                .Enrich.WithProperty("environment", ApplicationEnvironment.Environment)
                .Enrich.WithProperty("started-at", ApplicationInformation.StartedAt);

            if (productName != default)
            {
                config.Enrich.WithProperty("product-name", productName);
            }
        }

        private static void SetupConsole(IConfigurationRoot configRoot, LoggerConfiguration config)
        {
            var disableConsole = configRoot["disableConsoleLogOutput"];
            if (disableConsole?.ToLower() != "false")
            {
                config.WriteTo.Console();
            }
            else
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Log output to console is disabled.");
                Console.ForegroundColor = color;
                Console.WriteLine(
                    "To enable output please change setting 'disableConsoleLogOutput' to 'true' (see config or environment variables)");
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


            if (elasticsearchUrlsConfig?.ElasticsearchNodeUrls != null && elasticsearchUrlsConfig.ElasticsearchNodeUrls.Any())
            {
                var indexPrefix = elasticsearchUrlsConfig?.IndexPrefixName ?? "logs";

                config.WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(elasticsearchUrlsConfig.ElasticsearchNodeUrls.Select(u => new Uri(u)))
                    {
                        AutoRegisterTemplate = true,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                        IndexDecider = (e, o) => $"{indexPrefix}-{o.Date:yyyy-MM-dd}",
                    });
            }
        }

        private sealed class ElasticsearchUrlsConfig
        {
            public IReadOnlyCollection<string> ElasticsearchNodeUrls { get; set; }
            public string IndexPrefixName { get; set; }
        }

        private sealed class ElasticsearchConfig
        {
            public ElasticsearchUrlsConfig ElasticsearchLogs { get; set; }
        }

    }
}