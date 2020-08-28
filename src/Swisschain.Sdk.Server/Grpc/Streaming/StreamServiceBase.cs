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
        where TStreamItemCollection : class, IStreamItemCollection<TStreamItem, TStreamItemId>
        where TStreamItemId : IComparable<TStreamItemId>, IComparable
        where TStreamItem : IStreamItem<TStreamItemId>
    {
        private readonly ILogger _logger;
        private readonly Timer _checkTimer;
        private readonly Timer _pingTimer;

        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly List<StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>> _streamList =
            new List<StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>>();

        private readonly CancellationTokenSource _cancellationTokenSource;

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

        /// <summary>
        /// Use this method to write to stream directly from database
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task WriteToStreamHistorical(TStreamItemCollection data)
        {
            _readerWriterLock.EnterReadLock();

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
                _readerWriterLock.ExitReadLock();
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

            _readerWriterLock.EnterReadLock();

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
                _readerWriterLock.ExitReadLock();
            }
        }

        public StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> RegisterStream(StreamInfo<TStreamItemCollection> streamInfo,
            StreamFilterBase<TStreamItem, TStreamItemId> filter)
        {
            var data = StreamData<TStreamItemCollection, TStreamItem, TStreamItemId>.Create(
                streamInfo,
                filter,
                this._logger,
                this.WriteToStream);

            _readerWriterLock.EnterWriteLock();

            try
            {
                _streamList.Add(data);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }

            return data;
        }

        public void SwitchToReady(StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> streamData)
        {
            streamData.ProducerConsumerQueue.Start();
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _readerWriterLock.EnterWriteLock();

            try
            {
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
                _readerWriterLock.ExitWriteLock();
            }

            _cancellationTokenSource.Cancel();
            _checkTimer.Dispose();
            _pingTimer?.Dispose();
            _readerWriterLock.Dispose();
        }

        private void RemoveStream(StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> streamData)
        {
            streamData.CompletionTask.TrySetResult(1);

            _readerWriterLock.EnterWriteLock();

            try
            {
                _streamList.Remove(streamData);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }

            _logger.LogDebug($"StreamService<{typeof(TStreamItemCollection).Name}> Remove stream connection (peer: {streamData.Peer})");
        }

        private async Task WriteToStream(TStreamItemCollection data, StreamData<TStreamItemCollection, TStreamItem, TStreamItemId> streamData)
        {
            try
            {
                var processedData = ProcessDataBeforeSend(data, streamData);
                //Skip already processed messages
                if (!data.StreamItems.Any() ||
                    data.StreamItems.All(y => y.StreamItemId.CompareTo(streamData.Cursor) < 0))
                {
                    _logger.LogWarning("StreamService<{typeof(TStreamItemCollection).Name}> Skipped message during streaming {@context}",
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
                RemoveStream(streamData);
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

                foreach (var streamData in streamsToRemove)
                {
                    RemoveStream(streamData);
                }

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
            _readerWriterLock.EnterReadLock();

            try
            {
                foreach (var streamData in _streamList)
                {
                    var instance = streamData.LastSentData ?? Activator.CreateInstance<TStreamItemCollection>();

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
                _readerWriterLock.ExitReadLock();
            }
        }
    }
}
