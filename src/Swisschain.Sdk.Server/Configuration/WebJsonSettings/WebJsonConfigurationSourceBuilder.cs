using System;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    public sealed class WebJsonConfigurationSourceBuilder
    {
        public string Url { get; set; }
        
        public bool IsOptional { get; set; }
        
        public string Version { get; set; }

        public TimeSpan Timeout { get; set; }
    }
}