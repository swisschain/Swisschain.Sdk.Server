using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Swisschain.Sdk.Server.Configuration
{
    public class FileJsonSettingsLocations
    {
        public FileJsonSettingsLocations()
        {
            SecretsFilePath = new string[0];
            SettingsFilePath = new string[0];
        }
        
        public static FileJsonSettingsLocations BindDefault()
        {
            var result = new FileJsonSettingsLocations();
            
            new ConfigurationBuilder()
                .AddJsonFile("settings/settingslocator.json", optional: true)
                .AddEnvironmentVariables()
                .Build()
                .Bind(result);

            return result;
        }
        
        public string[] SecretsFilePath { get; set; }
        public string[] SettingsFilePath { get; set; }
    }
}