using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;
using Xunit;

namespace Swisschain.Sdk.Server.Test
{
    public class WithWebJsonConfigurationSourceTest
    {
        [Fact(Skip = "Use to debug issues with config reading")]
        public void BuildHostWithSwisschainServiceTest()
        {
            var builder = new HostBuilder()
                .SwisschainService<Startup>(options =>
                {
                    var remoteSettingsUrl = ApplicationEnvironment.Config["RemoteSettingsUrl"];

                    if (remoteSettingsUrl != default)
                    {
                        options.WithWebJsonConfigurationSource(webJsonOptions =>
                        {
                            webJsonOptions.Url = remoteSettingsUrl;
                            webJsonOptions.IsOptional = ApplicationEnvironment.IsDevelopment;
                            webJsonOptions.Version = ApplicationInformation.AppVersion;
                        });
                    }
                });

            builder.Build();
        }
    }

    public class Startup : SwisschainStartup<WalletManagerConfig>
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }
    }

    public class WalletManagerConfig
    {
        public DbConfig Db { get; set; }

        public string SeqUrl { get; set; }

        public BlockchainSettings BlockchainSettings { get; set; }
    }

    public class DbConfig
    {
        public string ConnectionString { get; set; }
    }

    public class BlockchainSettings
    {
        public string BlockchainSignFacadeUrl { get; set; }

        public Blockchain[] Blockchains { get; set; }
    }

    public class Blockchain
    {
        public string BlockchainId { get; set; }
        public string BlockchainApi { get; set; }
    }
}
