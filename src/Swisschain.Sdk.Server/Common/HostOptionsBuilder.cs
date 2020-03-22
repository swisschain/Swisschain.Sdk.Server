using System;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;

namespace Swisschain.Sdk.Server.Common
{
    public sealed class HostOptionsBuilder
    {
        public HostOptionsBuilder()
        {
            HttpPort = 5000;
            GrpcPort = 5001;
        }

        internal ILoggerFactory LoggerFactory { get; private set; }

        internal int HttpPort { get; set; }

        internal int GrpcPort { get; set; }

        internal Action<WebJsonConfigurationSourceBuilder> WebJsonConfigurationSourceBuilder { get; private set; }

        public HostOptionsBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;

            return this;
        }

        public HostOptionsBuilder UsePorts(int httpPort, int grpcPort)
        {
            HttpPort = httpPort;
            GrpcPort = grpcPort;

            return this;
        }

        /// <summary>
        /// Add web json configuration source - a app settings json accessible by an url.
        /// </summary>
        /// <param name="remoteSettingsUrl">If null web json configuration source will not be used</param>
        public HostOptionsBuilder WithWebJsonConfigurationSource(string remoteSettingsUrl)
        {
            if (remoteSettingsUrl != default)
            {
                WebJsonConfigurationSourceBuilder = options =>
                {
                    options.Url = remoteSettingsUrl;
                    options.IsOptional = ApplicationEnvironment.IsDevelopment;
                    options.Version = ApplicationInformation.AppVersion;
                };
            }
            
            return this;
        }

        /// <summary>
        /// Add web json configuration source - a app settings json accessible by an url.
        /// </summary>
        public HostOptionsBuilder WithWebJsonConfigurationSource(Action<WebJsonConfigurationSourceBuilder> builder)
        {
            WebJsonConfigurationSourceBuilder = builder;

            return this;
        }
    }
}