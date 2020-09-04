using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Grpc.Streaming;

namespace Swisschain.Sdk.Server.Test.Common.Grpc.Streaming
{
    public class StreamServiceExample : StreamServiceBase<StreamItemCollection, StreamItem, long>
    {
        private readonly Func<StreamData<StreamItemCollection, StreamItem, long>, Task> _afterStreamRemovedDelegate;
        private readonly Func<StreamData<StreamItemCollection, StreamItem, long>, Task> _beforeStreamRegisteredDelegate;

        public StreamServiceExample(
            ILogger logger, 
            bool needPing = false, 
            int pingPeriodMs = 30_000,
            Func<StreamData<StreamItemCollection, StreamItem, long>, Task> afterStreamRemovedDelegate = null,
            Func<StreamData<StreamItemCollection, StreamItem, long>, Task> beforeStreamRegisteredDelegate = null) 
            : base(logger, needPing, pingPeriodMs)
        {
            _afterStreamRemovedDelegate = afterStreamRemovedDelegate ?? base.AfterStreamRemoved;
            _beforeStreamRegisteredDelegate = beforeStreamRegisteredDelegate ?? base.BeforeStreamRegistered;
        }

        protected override Task AfterStreamRemoved(StreamData<StreamItemCollection, StreamItem, long> streamData)
        {
            return _afterStreamRemovedDelegate(streamData);
        }

        protected override Task BeforeStreamRegistered(StreamData<StreamItemCollection, StreamItem, long> streamData)
        {
            return _beforeStreamRegisteredDelegate(streamData);
        }
    }
}