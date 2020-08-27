using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Grpc.Abstractions;

namespace Swisschain.Sdk.Server.Grpc.Streaming
{
    internal class ProducerConsumerQueue<TStreamItemCollection, TStreamItem, TStreamItemId> : IDisposable
        where TStreamItemCollection : class, IStreamItemCollection<TStreamItem, TStreamItemId>
        where TStreamItemId : IComparable<TStreamItemId>, IComparable
        where TStreamItem : IStreamItem<TStreamItemId>
    {
        private readonly ILogger _logger;
        private readonly StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> _owner;
        private readonly Func<TStreamItemCollection, StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>, Task> _processFunc;
        private readonly EventWaitHandle _wh = new AutoResetEvent(false);
        private Task _worker;
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
        private readonly MinBinaryHeap<TStreamItemCollection, TStreamItem, TStreamItemId> _queue = 
            new MinBinaryHeap<TStreamItemCollection, TStreamItem, TStreamItemId>(128);
        private readonly object _initLocker = new object();
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ProducerConsumerQueue(
            ILogger logger, 
            StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> owner,
            Func<TStreamItemCollection, StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>, Task> processFunc)
        {
            _logger = logger;
            _owner = owner;
            _processFunc = processFunc;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void EnqueueTask(TStreamItemCollection task)
        {
            _locker.Wait();

            try
            {
                _queue.Enqueue(task);
            }
            finally
            {
                _locker.Release();
            }

            _wh.Set();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();      // Signal the consumer to exit.
            _wh.Set();
            _worker?.Wait();         // Wait for the consumer's thread to finish.
            _wh.Close();            // Release any OS resources.
            _worker?.Dispose();
            _cancellationTokenSource.Dispose();
            _wh.Dispose();
            _locker.Dispose();
        }

        private async Task Work()
        {
            while (true)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    return;

                TStreamItemCollection task = null;
                await _locker.WaitAsync();

                try
                {
                    if (_queue.Count > 0)
                    {
                        task = _queue.Dequeue();
                        
                        await _processFunc(task, _owner);
                    }
                }
                finally
                {
                    _locker.Release();
                }

                if (task == null)
                {
                    _wh.WaitOne();         // No more tasks - wait for a signal
                }
            }
        }

        public void Start()
        {
            lock (_initLocker)
            {
                if (_worker == null)
                {
                    _worker = Task.Run(Work);
                }
            }
        }
    }
}
