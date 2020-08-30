using Swisschain.Sdk.Server.Grpc.Streaming;

namespace Swisschain.Sdk.Server.Test.Common.Grpc.Streaming
{
    public class Filter : StreamFilterBase<StreamItem, long>
    {
        public override bool IsMatched(StreamItem item)
        {
            return true;
        }
    }
}