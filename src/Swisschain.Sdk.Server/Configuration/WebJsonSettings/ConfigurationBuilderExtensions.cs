using System;
using Microsoft.Extensions.Configuration;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddWebJsonConfiguration(
            this IConfigurationBuilder builder, 
            Action<WebJsonConfigurationSourceBuilder> optionsAction)
        {
            return builder.Add(new WebJsonConfigurationSource(optionsAction));
        }

        public static IConfigurationBuilder AddWebJsonConfiguration(this IConfigurationBuilder builder, 
            string url, 
            string version = default, 
            bool isOptional = false,
            TimeSpan timeout = default)
        {
            return builder.Add(new WebJsonConfigurationSource(options =>
            {
                options.Url = url;
                options.Version = version;
                options.IsOptional = isOptional;
            }));
        }
    }
}