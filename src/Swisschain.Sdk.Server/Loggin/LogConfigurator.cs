using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Swisschain.Sdk.Server.Common;

namespace Swisschain.Sdk.Server.Loggin
{
    public static class LogConfigurator
    {
        public static ILoggerFactory Configure(string productName = default, string seqUrl = default)
        {
            var configRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{ApplicationEnvironment.Environment}.json", optional: true)
                .Build();

            var config = new LoggerConfiguration()
                .ReadFrom.Configuration(configRoot)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionData()
                .Enrich.WithCorrelationIdHeader()
                .Enrich.WithProperty("app-name", ApplicationInformation.AppName)
                .Enrich.WithProperty("app-version", ApplicationInformation.AppVersion)
                .Enrich.WithProperty("host-name", ApplicationEnvironment.HostName)
                .Enrich.WithProperty("environment", ApplicationEnvironment.Environment)
                .Enrich.WithProperty("started-at", ApplicationInformation.StartedAt)
                .WriteTo.Console();

            if (productName != default)
            {
                config.Enrich.WithProperty("product-name", productName);
            }

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