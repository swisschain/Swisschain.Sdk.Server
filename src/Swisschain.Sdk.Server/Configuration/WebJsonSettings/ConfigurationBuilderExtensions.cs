using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddWebJsonConfiguration(this IConfigurationBuilder builder, 
            HttpClient client,
            string url,
            bool isOptional = false)
        {
            try
            {
                var stream = client.GetStreamAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();

                builder.AddJsonStream(stream);
            }
            catch (HttpRequestException ex) when (isOptional)
            {
                Log.Warning(ex, "Failed to load optional remote settings, skipping.");
            }

            return builder;
        }
    }
}