using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Swisschain.Sdk.Server.Test
{
    public delegate void MessageReceived<TStreamItemCollection>(object sender, TStreamItemCollection e);

    public class ServerStreamWriterFake : IServerStreamWriter<StreamItemCollection>, IDisposable
    {
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
        public List<StreamItemCollection> Messages { get; private set; } = new List<StreamItemCollection>();
        
        public Task WriteAsync(StreamItemCollection message)
        {
            _locker.WaitAsync();

            Messages.Add(message);

            OnMessageReceived(message);

            _locker.Release();

            return Task.CompletedTask;
        }

        public event MessageReceived<StreamItemCollection> MessageReceived;


        public WriteOptions WriteOptions { get; set; }

        protected virtual void OnMessageReceived(StreamItemCollection e)
        {
            MessageReceived?.Invoke(this, e);
        }

        public void Dispose()
        {
            _locker?.Dispose();
            if (MessageReceived != null)
                foreach (var d in MessageReceived.GetInvocationList())
                    MessageReceived -= (d as MessageReceived<StreamItemCollection>);
        }
    }
}