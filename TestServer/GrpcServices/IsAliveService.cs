using System;
using System.Threading.Tasks;
using Grpc.Core;
using Swisschain.Sirius.Api.ApiContract.Monitoring;

namespace TestServer.GrpcServices
{
    public class IsAliveService : MonitoringService.MonitoringServiceBase
    {
        public override Task<IsAliveResponse> IsAlive(IsAliveRequest request, ServerCallContext context)
        {
            throw new InvalidOperationException("WTF");
        }
    }
}