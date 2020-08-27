using System.Linq;
using Swisschain.Sdk.Server.Grpc.Streaming;
using Xunit;

namespace Swisschain.Sdk.Server.Test
{
    public class MinBinaryHeapTests
    {
        [Fact]
        public void TestOrder()
        {
            var heap =
                new MinBinaryHeap<StreamItemCollection, StreamItem, long>(128);

            var array = new StreamItemCollection[]
            {
                new StreamItemCollection(new []
                {
                    new StreamItem()
                    {
                        StreamItemId = 100,
                    }
                }),
                new StreamItemCollection(new []
                {
                    new StreamItem()
                    {
                        StreamItemId = 90,
                    }
                })
                ,
                new StreamItemCollection(new []
                {
                    new StreamItem()
                    {
                        StreamItemId = 80,
                    }
                })
                ,
                new StreamItemCollection(new []
                {
                    new StreamItem()
                    {
                        StreamItemId = 70,
                    }
                })
                ,
                new StreamItemCollection(new []
                {
                    new StreamItem()
                    {
                        StreamItemId = 60,
                    }
                })
                ,
                new StreamItemCollection(new []
                {
                    new StreamItem()
                    {
                        StreamItemId = 50,
                    }
                })
                ,
                new StreamItemCollection(new []
                {
                    new StreamItem()
                    {
                        StreamItemId = 40,
                    }
                })
            };

            foreach (var item in array)
            {
                heap.Enqueue(item);
            }

            var min = long.MinValue;
            //For Debug
            //var list = new List<WithdrawalUpdateArrayResponse>();
            while (heap.Count != 0)
            {
                var item = heap.Dequeue();
                //list.Add(item);
                Assert.True(min < item.StreamItems.First().StreamItemId);
                min = item.StreamItems.First().StreamItemId;
            }
        }

        [Fact]
        public void TestLimits()
        {
            var heap =
                new MinBinaryHeap<StreamItemCollection, StreamItem, long>(128);
            
            for (int i = 1024; i >0; i--)
            {
                heap.Enqueue(new StreamItemCollection(new[]
                {
                    new StreamItem()
                    {
                        StreamItemId = i
                    },
                }));
            }


            var min = long.MinValue;
            while (heap.Count != 0)
            {
                var item = heap.Dequeue();
                Assert.True(min < item.StreamItems.First().StreamItemId);
                min = item.StreamItems.First().StreamItemId;
            }

            Assert.True(heap.Count == 0);
        }
    }
}
