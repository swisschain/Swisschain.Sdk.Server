using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Grpc.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Swisschain.Sdk.Server.Grpc.Streaming
{
    public class StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> : StreamInfo<TStreamItemCollection>, IDisposable
        where TStreamItemId : IComparable, IComparable<TStreamItemId>
        where TStreamItemCollection : class, IStreamItemCollection<TStreamItem, TStreamItemId>
        where TStreamItem : IStreamItem<TStreamItemId>
    {
        public StreamFilterBase<TStreamItem, TStreamItemId> Filter { get; set; }
        public TStreamItemCollection LastSentData { get; set; }
        public TStreamItemId Cursor { get; set; }

        internal Queue<TStreamItemCollection> Queue { get; set; }

        internal TaskCompletionSource<int> CompletionTask { get; set; }

        internal ProducerConsumerQueue<TStreamItemCollection, TStreamItem, TStreamItemId> ProducerConsumerQueue { get; set; }

        public static StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> Create(
            StreamInfo<TStreamItemCollection> streamInfo, 
            StreamFilterBase<TStreamItem, TStreamItemId> filter,
            ILogger logger,
            Func<TStreamItemCollection, StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>, Task> processFunc)
        {
            var streamData = new StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>
            {
                CompletionTask = new TaskCompletionSource<int>(),
                CancelationToken = streamInfo.CancelationToken,
                Stream = streamInfo.Stream,
                Keys = streamInfo.Keys,
                Peer = streamInfo.Peer,
                LastSentData = null,
                Filter = filter,
                Queue = new Queue<TStreamItemCollection>(),
                Cursor = default,
            };

            streamData.ProducerConsumerQueue = new ProducerConsumerQueue<TStreamItemCollection, TStreamItem, TStreamItemId>(
                logger, 
                streamData,
                processFunc);

            return streamData;
        }

        /// <summary>
        /// Wait on this particular task in order to keep stream connection open
        /// </summary>
        /// <returns></returns>
        public Task GetCompletionTask()
        {
            return this.CompletionTask.Task;
        }

        public void Dispose()
        {
            ProducerConsumerQueue?.Dispose();
        }
    }
}
