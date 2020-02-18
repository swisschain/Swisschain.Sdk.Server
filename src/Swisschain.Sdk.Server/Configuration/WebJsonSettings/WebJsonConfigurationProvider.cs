using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    internal class WebJsonConfigurationProvider : ConfigurationProvider
    {
        private readonly HttpClient _httpClient;

        public WebJsonConfigurationProvider(Action<WebJsonConfigurationSourceBuilder> optionsAction, HttpClient httpClient = default)
        {
            OptionsAction = optionsAction;

            _httpClient = httpClient ?? new HttpClient();
        }

        private Action<WebJsonConfigurationSourceBuilder> OptionsAction { get; }

        public override void Load()
        {
            var options = new WebJsonConfigurationSourceBuilder();

            this.OptionsAction.Invoke(options);

            var timeout = options.Timeout == TimeSpan.Zero ? TimeSpan.FromSeconds(5) : options.Timeout;
            var url = options.Url;
            
            if (options.Version != default)
            {
                url = url.Contains("?")
                    ? $"{url}&version={HttpUtility.UrlEncode(options.Version)}"
                    : $"{url}?version={HttpUtility.UrlEncode(options.Version)}";
            }

            try
            {
                using var cancellation = new CancellationTokenSource(timeout);
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = _httpClient.SendAsync(request, cancellation.Token).ConfigureAwait(false).GetAwaiter().GetResult();

                response.EnsureSuccessStatusCode();

                var settings = JObject.Parse(response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult());

                Data = settings.GetLeafValues().ToDictionary(
                    x => x.Path.Replace(".", ConfigurationPath.KeyDelimiter),
                    x => x.Value.ToString());
            }
            catch (HttpRequestException ex) when (options.IsOptional)
            {
                Log.Warning(ex, "Failed to load optional remote settings, skipping.");
            }
        }
    }
}