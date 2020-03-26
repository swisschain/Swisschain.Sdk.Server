using System;
using System.Collections.Generic;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    internal sealed class WebJsonConfigurationSourcesBuilder
    {
        private readonly List<Source> _sources = new List<Source>();
        
        public void Add(Source source)
        {
            _sources.Add(source);
        }

        public IReadOnlyCollection<Source> Sources => _sources;

        public sealed class Source
        {
            public string Url { get; set; }

            public bool IsOptional { get; set; }

            public TimeSpan Timeout { get; set; }
        }
    }
}