using System.Collections.Generic;
using Swisschain.Extensions.Grpc.Abstractions;

namespace Swisschain.Sdk.Server.Test.Common.Grpc.Streaming
{
    public class StreamItemCollection : IStreamItemCollection<StreamItem, long>
    {
        public StreamItemCollection()
        {
            this.StreamItems = new List<StreamItem>();
        }
        public StreamItemCollection(IReadOnlyCollection<StreamItem> collection)
        {
            this.StreamItems = collection;
        }
        public IReadOnlyCollection<StreamItem> StreamItems { get; private set; }
    }
}
