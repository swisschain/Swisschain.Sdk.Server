using System;
using Microsoft.Extensions.Logging;
using Serilog;
using Swisschain.Sdk.Server.Common;

namespace Swisschain.Sdk.Server.Loggin
{
    public static class LogConfigurator
    {
        public static ILoggerFactory Configure(string projectName, string seqUrl)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionData()
                .Enrich.WithCorrelationIdHeader()
                .Enrich.WithProperty("project-name", projectName)
                .Enrich.WithProperty("app-name", ApplicationInformation.AppName)
                .Enrich.WithProperty("app-version", ApplicationInformation.AppVersion)
                .Enrich.WithProperty("host-name", ApplicationInformation.HostName)
                .Enrich.WithProperty("environment", ApplicationInformation.Environment)
                .Enrich.WithProperty("started-at", ApplicationInformation.StartedAt)
                .WriteTo.Console()
                .WriteTo.Seq(seqUrl, period: TimeSpan.FromSeconds(1))
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal((Exception)e.ExceptionObject, "Application has been terminated unexpectedly");
                Log.CloseAndFlush();
            };

            return new LoggerFactory().AddSerilog();
        }
    }
}