using System;
using Microsoft.Extensions.Configuration;

namespace Swisschain.Sdk.Server.Configuration
{
    public class FileJsonConfigurationLocation
    {
        public FileJsonConfigurationLocation()
        {
            SecretFilePaths = Array.Empty<string>();
            SettingFilePaths = Array.Empty<string>();
        }
        
        public static FileJsonConfigurationLocation BindDefault()
        {
            var result = new FileJsonConfigurationLocation();
            
            new ConfigurationBuilder()
                .AddJsonFile("settings/settingslocator.json", optional: true)
                .AddEnvironmentVariables()
                .Build()
                .Bind(result);

            return result;
        }
        
        public string[] SecretFilePaths { get; set; }
        public string[] SettingFilePaths { get; set; }
        public bool ShouldLogSettings { get; set; }
    }
}