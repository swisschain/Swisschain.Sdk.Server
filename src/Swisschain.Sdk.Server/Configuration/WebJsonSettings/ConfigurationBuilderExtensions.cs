using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace Swisschain.Sdk.Server.Configuration.WebJsonSettings
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddWebJsonConfiguration(this IConfigurationBuilder builder, 
            HttpClient client,
            string url,
            bool isOptional = false)
        {
            try
            {
                var jsonData = client.GetStringAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();

                builder.AddJsonFile(new InMemoryFileProvider(jsonData), "nonsense.json", false, false);
            }
            catch (HttpRequestException ex) when (isOptional)
            {
                Log.Warning(ex, "Failed to load optional remote settings, skipping.");
            }

            return builder;
        }
        
        // we can't use AddJsonStream because we have to call configurationBuilder.Build() multiple times to substitute secrets
        // it's not possible until https://github.com/dotnet/runtime/issues/43788 is fixed
        internal class InMemoryFileProvider : IFileProvider
        {
            private class InMemoryFile : IFileInfo
            {
                private readonly byte[] data;
                public InMemoryFile(string json) => data = Encoding.UTF8.GetBytes(json);
                public Stream CreateReadStream() => new MemoryStream(data);
                public bool Exists => true;
                public long Length => data.Length;
                public string PhysicalPath => null;
                public string Name => null;
                public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;
                public bool IsDirectory => false;
            }

            private readonly IFileInfo fileInfo;
            public InMemoryFileProvider(string json) => this.fileInfo = new InMemoryFile(json);
            public IFileInfo GetFileInfo(string _) => this.fileInfo;
            public IDirectoryContents GetDirectoryContents(string _) => null;
            public IChangeToken Watch(string _) => NullChangeToken.Singleton;
        }
    }
}