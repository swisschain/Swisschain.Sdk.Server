using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Serilog;

[assembly: InternalsVisibleTo("Swisschain.Sdk.Server.Test")]
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
            var regex = new Regex("[\\[](.*)[\\]]", RegexOptions.Multiline);
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
                    x =>
                    {
                        var pathDelimited = x.Path.Replace(".", ConfigurationPath.KeyDelimiter);
                        pathDelimited = regex.Replace(pathDelimited, match =>
                        {
                            if (match.Groups.Count == 2)
                                return $":{match.Groups[1].Value}";
                            
                            return match.Value;
                        });
                        return pathDelimited;
                    },
                    x => x.Value.ToString());
            }
            catch (HttpRequestException ex) when (options.IsOptional)
            {
                Log.Warning(ex, "Failed to load optional remote settings, skipping.");
            }
        }
    }
}