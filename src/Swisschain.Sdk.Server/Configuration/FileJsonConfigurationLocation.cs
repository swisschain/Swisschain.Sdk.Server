using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Swisschain.Sdk.Server.Configuration
{
    public class FileJsonConfigurationLocation
    {
        public FileJsonConfigurationLocation()
        {
            SecretFilePaths = new List<string>();
            SettingFilePaths = new List<string>();
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
        
        public IList<string> SecretFilePaths { get; set; }
        public IList<string> SettingFilePaths { get; set; }
        public bool ShouldLogSettings { get; set; }
    }
}