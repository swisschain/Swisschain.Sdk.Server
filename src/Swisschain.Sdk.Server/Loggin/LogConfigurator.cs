using System;
using Microsoft.Extensions.Logging;
using Serilog;
using Swisschain.Sdk.Server.Common;

namespace Swisschain.Sdk.Server.Loggin
{
    public static class LogConfigurator
    {
        public static ILoggerFactory Configure(string projectName = default, string seqUrl = default)
        {
            var config = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionData()
                .Enrich.WithCorrelationIdHeader()
                .Enrich.WithProperty("app-name", ApplicationInformation.AppName)
                .Enrich.WithProperty("app-version", ApplicationInformation.AppVersion)
                .Enrich.WithProperty("host-name", ApplicationEnvironment.HostName)
                .Enrich.WithProperty("environment", ApplicationEnvironment.Environment)
                .Enrich.WithProperty("started-at", ApplicationInformation.StartedAt)
                .WriteTo.Console();

            if (projectName != default)
            {
                config.Enrich.WithProperty("project-name", projectName);
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