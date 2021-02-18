using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput
{
    class RingBuffer<T>
    {
        public int Size { get; private set; }
        public T this[int key]
        {
            get => _data[(key + _iter + 1) % Size];
            set => _data[(key + _iter + 1) % Size] = value;
        }

        private T[] _data;
        private int _iter;

        public RingBuffer(int size)
        {
            Size = size;
            _data = new T[Size];
            _iter = 0;
        }

        public void Push(T item)
        {
            _iter++;
            while (_iter >= Size)
                _iter -= Size;
            _data[_iter] = item;
        }
    }
}
