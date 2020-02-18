using System;
using Microsoft.Extensions.Configuration;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    internal class WebJsonConfigurationSource : IConfigurationSource
    {
        private readonly Action<WebJsonConfigurationSourceBuilder> _optionsAction;

        public WebJsonConfigurationSource(Action<WebJsonConfigurationSourceBuilder> optionsAction)
        {
            _optionsAction = optionsAction;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new WebJsonConfigurationProvider(_optionsAction);
        }
    }
}