using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Grpc.Streaming;
using Swisschain.Sdk.Server.Test.Common.Grpc.Streaming;
using System.Threading;
using System.Threading.Tasks;

namespace Swisschain.Sdk.Server.Benchmark.Grpc.Streaming
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [CsvExporter]
    [CsvMeasurementsExporter]
    [HtmlExporter]
    [PlainExporter]
    [GcServer(true)]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class StreamServiceBenchmark
    {
        private StreamServiceExample _streamService;
        [Params(100, 1_000, 10_000, 100_000, 1_000_000)]
        public int _totalCount;
        private StreamItemCollection[] _arrayOfData;
        private StreamData<StreamItemCollection, StreamItem, long> _streamData;

        [GlobalSetup]
        public void Setup()
        {
            _arrayOfData = new StreamItemCollection[_totalCount];

            for (int i = 0; i < _totalCount; i++)
            {
                _arrayOfData[i] = new StreamItemCollection(new []
                {
                    new StreamItem()
                    {
                        StreamItemId = i
                    }
                });
            }

            var loggerFactory = new LoggerFactory();
            _streamService = new StreamServiceExample(loggerFactory.CreateLogger("StreamServiceBenchmark"), false);
            var cts = new CancellationTokenSource();
            var serverStreamWriter = new ServerStreamWriterFake();
            var streamInfo = new StreamInfo<StreamItemCollection>()
            {
                CancelationToken = cts.Token,
                Keys = new[] {"tenantId"},
                Peer = "127.0.0.1:5000",
                Stream = serverStreamWriter
            };

            _streamData = _streamService.RegisterStream(streamInfo, new Filter());
            _streamService.SwitchToReady(_streamData);
        }

        [GlobalCleanup]
        public void CleanUp()
        {
            _streamService.Dispose();
        }

        [Benchmark()]
        public void Base()
        {
            var counter = 0;
            var tcs = new TaskCompletionSource<int>();
            var stream = _streamData.Stream as ServerStreamWriterFake;
            MessageReceived<StreamItemCollection> messageReceivedFunc = (sender, collection) =>
            {
                counter++;

                if (counter == _totalCount)
                    tcs.TrySetResult(1);
            };

            stream.MessageReceived += messageReceivedFunc;

            for (int i = 0; i < _totalCount; i++)
            {
                var item = _arrayOfData[i];
                _streamService.WriteToStreamActual(item);
            }

            tcs.Task.Wait(20_000);
            
            _streamData.Cursor = default;
            _streamData.LastSentData = null;
            stream.Messages.Clear();
            stream.MessageReceived -= messageReceivedFunc;
        }

        //[Benchmark]
        //public void Slow() => Thread.Sleep(200);

        //[Benchmark]
        //public void Fast() => Thread.Sleep(50);
    }
}
