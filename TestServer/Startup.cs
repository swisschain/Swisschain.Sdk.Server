using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Swisschain.Sdk.Server.Common;
using TestServer.Configuration;
using Microsoft.AspNetCore.Builder;
using TestServer.GrpcServices;

namespace TestServer
{

    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        protected override void RegisterEndpoints(IEndpointRouteBuilder endpoints)
        {
            base.RegisterEndpoints(endpoints);
            endpoints.MapGrpcService<IsAliveService>();
        }
    }
}