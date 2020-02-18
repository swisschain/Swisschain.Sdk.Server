using System;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;

namespace Swisschain.Sdk.Server.Common
{
    public sealed class HostOptionsBuilder
    {
        public HostOptionsBuilder()
        {
            RestPort = 5000;
            GrpcPort = 5001;
        }

        internal ILoggerFactory LoggerFactory { get; private set; }

        internal int RestPort { get; private set; }

        internal int GrpcPort { get; private set; }

        internal Action<WebJsonConfigurationSourceBuilder> WebJsonConfigurationSourceBuilder { get; private set; }

        public HostOptionsBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;

            return this;
        }

        public HostOptionsBuilder UsePorts(int restPort, int grpcPort)
        {
            RestPort = restPort;
            GrpcPort = grpcPort;

            return this;
        }

        public HostOptionsBuilder WithWebJsonConfigurationSource(Action<WebJsonConfigurationSourceBuilder> builder)
        {
            WebJsonConfigurationSourceBuilder = builder;

            return this;
        }
    }
}