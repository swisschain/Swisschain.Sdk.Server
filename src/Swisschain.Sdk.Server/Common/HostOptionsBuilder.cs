using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Configuration;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;

namespace Swisschain.Sdk.Server.Common
{
    public sealed class HostOptionsBuilder
    {
        public HostOptionsBuilder()
        {
            HttpPort = 5000;
            GrpcPort = 5001;
            WebJsonConfigurationSourcesBuilder = new WebJsonConfigurationSourcesBuilder();
        }

        internal ILoggerFactory LoggerFactory { get; private set; }

        internal int HttpPort { get; set; }

        internal int GrpcPort { get; set; }

        internal WebJsonConfigurationSourcesBuilder WebJsonConfigurationSourcesBuilder { get; }
        
        internal FileJsonSettingsLocations FileJsonSettingsLocations { get; private set; }

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
        /// Adds a web json configuration source - an app settings json accessible by an url.
        /// </summary>
        public HostOptionsBuilder AddWebJsonConfigurationSource(string url, bool isOptional = true, TimeSpan timeout = default)
        {
            WebJsonConfigurationSourcesBuilder.Add(new WebJsonConfigurationSourcesBuilder.Source
            {
                Url = url,
                IsOptional = isOptional,
                Timeout = timeout
            });

            return this;
        }

        /// <summary>
        /// Adds a web json configuration sources - an app settings json accessible by an url.
        /// </summary>
        public HostOptionsBuilder AddWebJsonConfigurationSources(IReadOnlyCollection<string> urls, bool areOptional = true, TimeSpan timeout = default)
        {
            foreach (var url in urls)
            {
                AddWebJsonConfigurationSource(url, areOptional, timeout);
            }

            return this;
        }

        public HostOptionsBuilder AddFileJsonSettingsLocations(FileJsonSettingsLocations locations)
        {
            FileJsonSettingsLocations = locations;
            return this;
        }
    }
}