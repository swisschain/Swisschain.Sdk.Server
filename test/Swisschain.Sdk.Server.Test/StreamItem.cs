using Swisschain.Extensions.Grpc.Abstractions;

namespace Swisschain.Sdk.Server.Test
{
    public class StreamItem : IStreamItem<long>
    {
        public long StreamItemId { get; set; }
    }
}
