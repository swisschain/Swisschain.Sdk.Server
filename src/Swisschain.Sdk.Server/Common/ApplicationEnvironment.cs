using Microsoft.Extensions.Configuration;

namespace Swisschain.Sdk.Server.Common
{
    public static class ApplicationEnvironment
    {
        static ApplicationEnvironment()
        {
            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();

            Config = configBuilder.Build();
            Environment = Config["ASPNETCORE_ENVIRONMENT"];
            HostName = Config["HOSTNAME"];
        }

        /// <summary>
        /// This config includes only settings provided via environment variables.
        /// Intended to read setting from the environment until main app configuration is built.
        /// </summary>
        public static IConfigurationRoot Config { get; }

        public static string Environment { get; }

        public static string HostName { get; }

        public static bool IsDevelopment => Environment == "Development";
        
        public static bool IsStaging => Environment == "Staging";

        public static bool IsProduction => Environment == "Production";
    }
}