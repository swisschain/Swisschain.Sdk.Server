using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;

namespace Swisschain.Sdk.Server.Logging
{
    internal static class RequestIdLoggerConfigurationExtensions
    {
        public static LoggerConfiguration WithRequestIdHeader(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            string headerKey = "x-request-id")
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }
            
            return enrichmentConfiguration.With((ILogEventEnricher) new RequestIdHeaderEnricher(headerKey));
        }
    }
}