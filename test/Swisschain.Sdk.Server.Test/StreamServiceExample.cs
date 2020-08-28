using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Grpc.Streaming;

namespace Swisschain.Sdk.Server.Test
{
    public class StreamServiceExample : StreamServiceBase<StreamItemCollection, StreamItem, long>
    {
        public StreamServiceExample(ILogger logger, bool needPing = false, int pingPeriodMs = 30_000) : base(logger, needPing, pingPeriodMs)
        {
        }
    }
}