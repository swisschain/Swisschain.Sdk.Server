using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Swisschain.Extensions.Grpc.Abstractions;

[assembly: InternalsVisibleTo("Swisschain.Sdk.Server.Test")]
namespace Swisschain.Sdk.Server.Grpc.Streaming
{
    internal class MinBinaryHeap<TStreamItemCollection, TStreamItem, TStreamItemId>
        where TStreamItemCollection : class, IStreamItemCollection<TStreamItem, TStreamItemId>
        where TStreamItemId : IComparable<TStreamItemId>, IComparable
        where TStreamItem : IStreamItem<TStreamItemId>
    {
        private TStreamItemCollection[] _heap;
        private int _counter;

        public int Parent(int index) => (index - 1) / 2;
        public int LeftChild(int index) => 2 * index + 1;
        public int RightChild(int index) => 2 * index + 2;
        public TStreamItemCollection Root => _heap[0];

        public MinBinaryHeap(int heapSize)
        {
            _heap = new TStreamItemCollection[heapSize];
            _counter = 0;
        }

        public int Count => _counter;

        private MinBinaryHeap(TStreamItemCollection[] array)
        {
            _counter = array.Length;
            _heap = array;
            for (int i = _counter / 2; i >= 0; i--)
            {
                BubbleDown(i);
            }
        }

        public void Enqueue(TStreamItemCollection element)
        {
            if (_counter == _heap.Length)
            {
                var tmp = _heap;
                _heap = new TStreamItemCollection[_heap.Length * 2];
                Array.Copy(tmp, _heap, tmp.Length);
            }

            _heap[_counter] = element;
            BubbleUp(_counter);
            _counter++;
        }

        public TStreamItemCollection Dequeue()
        {
            var root = _heap[0];
            _heap[0] = _heap[_counter-1];
            _counter--;

            BubbleDown(0);

            if (_counter == 0 && _heap.Length > 128)
            {
                _heap = new TStreamItemCollection[128];
            }

            return root;
        }

        private void BubbleUp(int index)
        {
            var parentIndex = Parent(index);
            while (index > 0 && _heap[parentIndex].StreamItems.First()
                .StreamItemId.CompareTo(_heap[index]
                    .StreamItems.First()
                    .StreamItemId) > 0)
            {
                Swap(parentIndex, index);
                index = parentIndex;
                parentIndex = Parent(index);
            }
        }

        private void BubbleDown(int index)
        {
            var minIndex = index;

            while (true)
            {
                var l = LeftChild(minIndex);
                var r = RightChild(minIndex);

                if (l < _counter && _heap[l].StreamItems.First().StreamItemId.CompareTo(_heap[minIndex]
                    .StreamItems.First()
                    .StreamItemId) < 0)
                    minIndex = l;

                if (r < _counter && _heap[r].StreamItems.First().StreamItemId.CompareTo(_heap[minIndex]
                    .StreamItems.First()
                    .StreamItemId) < 0)
                    minIndex = r;

                if (minIndex == index)
                    return;

                Swap(minIndex, index);
                index = minIndex;
            }
        }

        private void Swap(int from, int to)
        {
            var temp = _heap[from];

            _heap[from] = _heap[to];
            _heap[to] = temp;
        }

        public static MinBinaryHeap<TSStreamItemCollection, TSStreamItem, TSStreamItemId> BuildHeap<TSStreamItemCollection, TSStreamItem, TSStreamItemId>(
            TSStreamItemCollection[] 
            array) 
            where TSStreamItemCollection : class, IStreamItemCollection<TSStreamItem, TSStreamItemId>
            where TSStreamItemId : IComparable<TSStreamItemId>, IComparable
            where TSStreamItem : IStreamItem<TSStreamItemId>
        {
            var heap = 
                new MinBinaryHeap<TSStreamItemCollection, TSStreamItem, TSStreamItemId>(array);

            return heap;
        }
    }
}
