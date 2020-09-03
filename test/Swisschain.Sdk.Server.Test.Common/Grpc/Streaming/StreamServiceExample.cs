using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Grpc.Streaming;

namespace Swisschain.Sdk.Server.Test.Common.Grpc.Streaming
{
    public class StreamServiceExample : StreamServiceBase<StreamItemCollection, StreamItem, long>
    {
        public StreamServiceExample(ILogger logger, bool needPing = false, int pingPeriodMs = 30_000) : base(logger, needPing, pingPeriodMs)
        {
        }
    }
}