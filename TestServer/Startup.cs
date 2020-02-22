using Microsoft.Extensions.Configuration;
using Swisschain.Sdk.Server.Common;
using TestServer.Configuration;

namespace TestServer
{

    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }
    }
}