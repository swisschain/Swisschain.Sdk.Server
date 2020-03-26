using System;
using System.Net.Http;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    internal static class WebJsonHttpClientProvider
    {
        public static readonly HttpClient DefaultClient;

        static WebJsonHttpClientProvider()
        {
            DefaultClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }
    }
}