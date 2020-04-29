using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Swisschain.Sdk.Server.Common;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    internal static class WebJsonHttpClientProvider
    {
        public static readonly HttpClient DefaultClient;

        static WebJsonHttpClientProvider()
        {
            var timeout = ApplicationEnvironment.Config.GetValue("RemoteSettingsReadTimeout", TimeSpan.FromSeconds(5));

            Console.WriteLine($"Env - RemoteSettingsReadTimeout: {timeout}");

            DefaultClient = new HttpClient
            {
                Timeout = timeout
            };
        }
    }
}