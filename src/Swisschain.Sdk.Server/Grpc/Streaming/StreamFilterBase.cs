using System;
using Swisschain.Extensions.Grpc.Abstractions;

namespace Swisschain.Sdk.Server.Grpc.Streaming
{
    public abstract class StreamFilterBase<TStreamItem, TStreamItemId>
        where TStreamItemId : IComparable, IComparable<TStreamItemId>
        where TStreamItem :  IStreamItem<TStreamItemId>
    {
        public abstract bool IsMatched(TStreamItem item);
    }
}
