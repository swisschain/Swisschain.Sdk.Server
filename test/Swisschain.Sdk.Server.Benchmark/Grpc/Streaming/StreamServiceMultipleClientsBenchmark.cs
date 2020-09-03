using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Grpc.Streaming;
using Swisschain.Sdk.Server.Test.Common.Grpc.Streaming;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

namespace Swisschain.Sdk.Server.Benchmark.Grpc.Streaming
{
    // Using percentiles for adequate timings representation
    [Config(typeof(Config))]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [CsvExporter]
    [CsvMeasurementsExporter]
    [HtmlExporter]
    [PlainExporter]
    [GcServer(true)]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class StreamServiceMultipleClientsBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {

                Add(
                    StatisticColumn.MValue,
                    StatisticColumn.Max,
                    StatisticColumn.Min);

            }
        }

        private StreamServiceExample _streamService;
        [Params(100, 1000, 10_000)]
        public int TotalCount;

        [Params(1, 10, 100)]
        public int ConnectedStreams;

        private StreamItemCollection[] _arrayOfData;
        private List<StreamData<StreamItemCollection, StreamItem, long>> _streamData;
        private TaskCompletionSource<int>[] _taskCompletionSources;
        private Task<int>[] _tasksToWait;
        private List<MessageReceived<StreamItemCollection>> _messageReceiveds;

        [GlobalSetup]
        public void Setup()
        {
            _arrayOfData = new StreamItemCollection[TotalCount];

            for (int i = 0; i < TotalCount; i++)
            {
                _arrayOfData[i] = new StreamItemCollection(new[]
                {
                    new StreamItem()
                    {
                        StreamItemId = i
                    }
                });
            }

            var loggerFactory = new LoggerFactory();
            _streamService = new StreamServiceExample(loggerFactory.CreateLogger("StreamServiceMultipleClientsBenchmark"), false);
            var cts = new CancellationTokenSource();

            var list = new List<StreamData<StreamItemCollection, StreamItem, long>>();
            for (int i = 0; i < ConnectedStreams; i++)
            {
                var serverStreamWriter = new ServerStreamWriterFake();
                var streamInfo = new StreamInfo<StreamItemCollection>()
                {
                    CancelationToken = cts.Token,
                    Keys = new[] { "tenantId" },
                    Peer = "127.0.0.1:5000",
                    Stream = serverStreamWriter
                };

                var streamDataItem = _streamService.RegisterStream(streamInfo, new Filter());
                _streamService.SwitchToReady(streamDataItem);
                list.Add(streamDataItem);
            }

            _streamData = list;
        }

        [GlobalCleanup]
        public void CleanUp()
        {
            _streamService.Dispose();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _taskCompletionSources = _streamData
                .Select(x => new TaskCompletionSource<int>())
                .ToArray();
            var counters = new int[_streamData.Count];
            _messageReceiveds = new List<MessageReceived<StreamItemCollection>>(_streamData.Count);
            for (int i = 0; i < _streamData.Count; i++)
            {
                var closure = i;
                var item = _streamData[closure];
                var stream = item.Stream as ServerStreamWriterFake;
                MessageReceived<StreamItemCollection> messageReceivedFunc = (sender, collection) =>
                {
                    counters[closure]++;

                    if (counters[closure] == TotalCount)
                        _taskCompletionSources[closure].TrySetResult(1);
                };

                stream.MessageReceived += messageReceivedFunc;
                _messageReceiveds.Add(messageReceivedFunc);
            }

            _tasksToWait = _taskCompletionSources.Select(x => x.Task).ToArray();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            for (int i = 0; i < _streamData.Count; i++)
            {
                var item = _streamData[i];
                item.Cursor = default;
                item.LastSentData = null;
                var stream = item.Stream as ServerStreamWriterFake;
                stream.Messages.Clear();
                stream.MessageReceived -= _messageReceiveds[i];
            }
        }

        [Benchmark()]
        public void Base()
        {
            for (int i = 0; i < TotalCount; i++)
            {
                var item = _arrayOfData[i];
                _streamService.WriteToStreamActual(item);
            }

            Task.WaitAll(_tasksToWait, 20_000);
        }

        //[Benchmark]
        //public void Slow() => Thread.Sleep(200);

        //[Benchmark]
        //public void Fast() => Thread.Sleep(50);
    }
}
