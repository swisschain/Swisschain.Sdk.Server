using Swisschain.Extensions.Grpc.Abstractions;

namespace Swisschain.Sdk.Server.Test.Common.Grpc.Streaming
{
    public class StreamItem : IStreamItem<long>
    {
        public long StreamItemId { get; set; }
    }
}
