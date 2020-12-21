using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Grpc.Abstractions;

namespace Swisschain.Sdk.Server.Grpc.Streaming
{
    public abstract class StreamServiceBase<TStreamItemCollection, TStreamItem, TStreamItemId> : IDisposable
        where TStreamItemCollection : class, IStreamItemCollection<TStreamItem, TStreamItemId>, new()
        where TStreamItemId : IComparable<TStreamItemId>, IComparable
        where TStreamItem : IStreamItem<TStreamItemId>
    {
        private readonly ILogger _logger;
        private readonly Timer _checkTimer;
        private readonly Timer _pingTimer;

        private readonly ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
        private readonly List<StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>> _streamList =
            new List<StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>>();

        private readonly CancellationTokenSource _cancellationTokenSource;
        private static readonly TStreamItemCollection _pingItem = new TStreamItemCollection();

        public StreamServiceBase(ILogger logger, bool needPing = false, int pingPeriodMs = 30_000)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger;
            _checkTimer = new Timer(x =>
                {
                    _logger.LogDebug($"StreamService<{typeof(TStreamItemCollection).Name}> checking streams");
                    var streamsRemovedCount = CheckStreams();
                    _logger.LogDebug($"StreamService<{typeof(TStreamItemCollection).Name}> {streamsRemovedCount} streams were removed");
                },
                new object(),
                0,
                10_000);

            if (needPing)
            {
                _pingTimer = new Timer(x =>
                {
                    Ping().GetAwaiter().GetResult();
                }, new object(), 0, pingPeriodMs);
            }
        }

        public virtual TStreamItemCollection ProcessDataBeforeSend(TStreamItemCollection data,
            StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> streamData)
        {
            return data;
        }

        protected virtual Task BeforeStreamRegistered(
            StreamInfo<TStreamItemCollection> streamInfo)
        {
            return Task.CompletedTask;
        }

        protected virtual Task AfterStreamRemoved(
            StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> streamData)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Use this method to write to stream directly from database
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task WriteToStreamHistorical(TStreamItemCollection data)
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                var items = _streamList.ToArray();

                items = items
                    .Where(x => !x.CancelationToken?.IsCancellationRequested ?? true)
                    // No need to filter from DB
                    //.Where(x => data.StreamItems.All(y => x.Filter.IsValid(y)))
                    .ToArray();

                foreach (var streamData in items)
                {
                    await WriteToStream(data, streamData);
                }

            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Messages are in proto
        /// </summary>
        /// <param name="data">Should contain only one item in collection packet</param>
        /// <returns></returns>
        public void WriteToStreamActual(TStreamItemCollection data)
        {
            if (data.StreamItems.Count != 1)
            {
                throw new ArgumentException("Data should contain only one element.", nameof(data));
            }

            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                var items = _streamList.ToArray();

                items = items
                    .Where(x => !x.CancelationToken?.IsCancellationRequested ?? true)
                    .Where(x => data.StreamItems.All(y => x.Filter.IsMatched(y)))
                    .ToArray();

                foreach (var streamData in items)
                {
                    streamData.ProducerConsumerQueue.EnqueueTask(data);
                }

            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        public async Task<StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>> RegisterStream(StreamInfo<TStreamItemCollection> streamInfo,
            StreamFilterBase<TStreamItem, TStreamItemId> filter)
        {
            await BeforeStreamRegistered(streamInfo);

            _logger.LogDebug($"StreamService<{typeof(TStreamItemCollection).Name}> Register stream connection (peer: {streamInfo.Peer})");

            var data = StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>.Create(
                streamInfo,
                filter,
                this._logger,
                this.WriteToStream);

            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                _streamList.Add(data);
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }

            return data;
        }

        public void SwitchToReady(StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> streamData)
        {
            streamData.ProducerConsumerQueue.Start();
        }

        public void Dispose()
        {
            IReadOnlyCollection<StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>> streams = null;
            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                _cancellationTokenSource.Cancel();
                _checkTimer.Dispose();
                _pingTimer?.Dispose();

                streams = _streamList.ToArray();

                foreach (var streamInfo in _streamList)
                {
                    streamInfo.CompletionTask.TrySetResult(1);
                    streamInfo.Dispose();
                    _logger.LogDebug(
                        $"StreamService<{typeof(TStreamItemCollection).Name}> Remove stream connection (peer: {streamInfo.Peer})");
                }
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }

            var tasks = streams.Select(AfterStreamRemoved).ToArray();

            Task.WaitAll(tasks, 60_000);
        }

        private async Task RemoveStream(StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> streamData)
        {
            streamData.CompletionTask.TrySetResult(1);

            try
            {
                _readerWriterLock.AcquireWriterLock(TimeSpan.FromSeconds(10));
            }
            catch (Exception e)
            {
                _logger.LogDebug(
                    $"StreamService<{typeof(TStreamItemCollection).Name}> Remove stream, error happened (peer: {streamData.Peer})", e);
                return;
            }

            try
            {
                _streamList.Remove(streamData);
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }

            await AfterStreamRemoved(streamData);

            _logger.LogDebug($"StreamService<{typeof(TStreamItemCollection).Name}> Remove stream connection (peer: {streamData.Peer})");
        }

        private async Task WriteToStream(TStreamItemCollection data, StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> streamData)
        {
            try
            {
                var processedData = ProcessDataBeforeSend(data, streamData);
                
                //Ping
                if (data.StreamItems == null || 
                    !data.StreamItems.Any())
                {
                    await streamData.Stream.WriteAsync(processedData);

                    return;

                }

                //Skip already processed messages
                if (data.StreamItems.All(y => y.StreamItemId.CompareTo(streamData.Cursor) < 0))
                {
                    _logger.LogWarning("Skipped message during streaming {@context}",
                        new
                        {
                            Peer = streamData.Peer,
                            Type = typeof(TStreamItemCollection),
                            Message = data
                        });

                    return;
                }

                streamData.LastSentData = processedData;
                streamData.Cursor = processedData.StreamItems.Any() ? processedData.StreamItems.Last().StreamItemId : streamData.Cursor;
                await streamData.Stream.WriteAsync(processedData);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    "Error happened during streaming {@context}",
                    new
                    {
                        Peer = streamData.Peer,
                        Type = typeof(TStreamItemCollection).Name
                    });
                await RemoveStream(streamData);
            }
        }

        private int CheckStreams()
        {
            var countToRemove = 0;

            try
            {
                var streamsToRemove = _streamList
                .Where(x => x.CancelationToken.HasValue && x.CancelationToken.Value.IsCancellationRequested)
                .ToList();

                var tasks = new List<Task>(streamsToRemove.Count);
                foreach (var streamData in streamsToRemove)
                {
                    tasks.Add(RemoveStream(streamData));
                }

                if (tasks.Any())
                    Task.WaitAll(tasks.ToArray());

                countToRemove = streamsToRemove.Count;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"StreamService<{typeof(TStreamItemCollection).Name}> Error happened during stream check");
            }

            return countToRemove;
        }

        private async Task Ping()
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                foreach (var streamData in _streamList)
                {
                    var instance = _pingItem;

                    try
                    {
                        await WriteToStream(instance, streamData);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Error happened during stream ping {@context}", new
                        {
                            streamData.Peer
                        });
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"StreamService<{typeof(TStreamItemCollection).Name}> Error happened during stream ping");
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }
    }
}
