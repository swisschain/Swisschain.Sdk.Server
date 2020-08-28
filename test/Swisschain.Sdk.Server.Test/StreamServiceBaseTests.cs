using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Grpc.Streaming;
using Xunit;

namespace Swisschain.Sdk.Server.Test
{
    public class StreamServiceBaseTests
    {
        [Fact]
        public async Task PingTest()
        {
            var loggerFactory = new LoggerFactory();
            var streamService = new StreamServiceExample(loggerFactory.CreateLogger("StreamServiceBaseTests"), true, 1_000);
            var cts = new CancellationTokenSource();
            var serverStreamWriter = new ServerStreamWriterFake();
            var streamInfo = new StreamInfo<StreamItemCollection>()
            {
                CancelationToken = cts.Token,
                Keys = new[] { "tenantId" },
                Peer = "127.0.0.1:5000",
                Stream = serverStreamWriter
            };

            var streamData = streamService.RegisterStream(streamInfo, new Filter());
            var completionTask = streamData.GetCompletionTask();
            streamService.SwitchToReady(streamData);
            streamService.WriteToStreamActual(new StreamItemCollection(new StreamItem[]
            {
                new StreamItem()
                {
                    StreamItemId = 10
                }, 
            }));

            
            var delayTask = Task.Delay(TimeSpan.FromSeconds(5));
            var firstCompleted = Task.WaitAny(new Task[] {delayTask, completionTask});

            Assert.Equal(0, firstCompleted);

            Assert.True(serverStreamWriter.Messages.Count >= 2);

            streamData.Dispose();
            serverStreamWriter.Dispose();
        }

        [Fact]
        public async Task WriteToActualTest()
        {
            var loggerFactory = new LoggerFactory();
            var streamService = new StreamServiceExample(loggerFactory.CreateLogger("StreamServiceBaseTests"), false);
            var cts = new CancellationTokenSource();
            var serverStreamWriter = new ServerStreamWriterFake();
            var streamInfo = new StreamInfo<StreamItemCollection>()
            {
                CancelationToken = cts.Token,
                Keys = new[] { "tenantId" },
                Peer = "127.0.0.1:5000",
                Stream = serverStreamWriter
            };

            var streamData = streamService.RegisterStream(streamInfo, new Filter());
            var tcs = new TaskCompletionSource<int>();
            var counter = 0;
            serverStreamWriter.MessageReceived += (sender, collection) =>
            {
                counter++;

                if (counter == 21)
                    tcs.TrySetResult(1);
            };
            var completionTask = streamData.GetCompletionTask();
            await streamService.WriteToStreamHistorical(new StreamItemCollection(new StreamItem[]
            {
                new StreamItem()
                {
                    StreamItemId = 10
                },
                new StreamItem()
                {
                    StreamItemId = 11
                },
                new StreamItem()
                {
                    StreamItemId = 12
                },
                new StreamItem()
                {
                    StreamItemId = 13
                },
            }));

            for (int i = 14; i < 24; i++)
            {
                streamService.WriteToStreamActual(new StreamItemCollection(new StreamItem[]
                {
                    new StreamItem()
                    {
                        StreamItemId = 14
                    },
                }));
            }

            streamService.SwitchToReady(streamData);

            for (int i = 24; i < 34; i++)
            {
                streamService.WriteToStreamActual(new StreamItemCollection(new StreamItem[]
                {
                    new StreamItem()
                    {
                        StreamItemId = 14
                    },
                }));
            }

            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(1));
            var firstCompleted = Task.WaitAny(new Task[] {  completionTask, tcs.Task, timeoutTask });

            Assert.True(firstCompleted < 2);

            Assert.True(serverStreamWriter.Messages.Count == 21);

            streamData.Dispose();
            serverStreamWriter.Dispose();
        }

        [Fact]
        public async Task WriteToActualManyThreadsTest()
        {
            var loggerFactory = new LoggerFactory();
            var streamService = new StreamServiceExample(loggerFactory.CreateLogger("StreamServiceBaseTests"), true, 10);
            var cts = new CancellationTokenSource();
            var serverStreamWriter = new ServerStreamWriterFake();
            var streamInfo = new StreamInfo<StreamItemCollection>()
            {
                CancelationToken = cts.Token,
                Keys = new[] { "tenantId" },
                Peer = "127.0.0.1:5000",
                Stream = serverStreamWriter
            };

            var streamData = streamService.RegisterStream(streamInfo, new Filter());
            var tcs = new TaskCompletionSource<int>();
            var completionTask = streamData.GetCompletionTask();

            streamService.SwitchToReady(streamData);

            var task1 = Task.Run(() =>
            {
                for (int i = 0; i < 680; i++)
                {
                    streamService.WriteToStreamActual(new StreamItemCollection(new StreamItem[]
                    {
                        new StreamItem()
                        {
                            StreamItemId = 14
                        },
                    }));
                }
            });

            var task2 = Task.Run(() =>
            {
                for (int i = 0; i < 680; i++)
                {
                    streamService.WriteToStreamActual(new StreamItemCollection(new StreamItem[]
                    {
                        new StreamItem()
                        {
                            StreamItemId = 14
                        },
                    }));
                }
            });

            await Task.WhenAll(task1, task2);
            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(10_000));
            var firstCompleted = Task.WaitAny(new Task[] { completionTask, tcs.Task, timeoutTask });

            streamData.Dispose();
            serverStreamWriter.Dispose();
        }
    }
}