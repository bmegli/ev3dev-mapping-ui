// Modified implementation from: 
// https://circularbuffer.codeplex.com/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace CircularBuffer
{
    public class CircularBuffer<T> : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable
    {
        private int capacity;
        private int size;
        private int head;
        private int tail;
        private T[] buffer;

		#region Added Functionality

		public T this[int i]
		{
			get
			{
				if(head+i<capacity)
					return buffer[head+i];
				else
					return buffer[head+i-capacity];
			}
			set
			{
				if (head + i < capacity)
					buffer [head + i] = value;
				else
					buffer [head + i - capacity] = value;
			}
		}

		public T[] CloneSubset(int index_from, int index_to)
		{
			T[] data = new T[index_to - index_from + 1];
			CopySubset(data, index_from, index_to);
			return data;
		}

		public void CopySubset(T[] array, int index_from, int index_to)
		{
			int count = index_to - index_from + 1;
			int buffer_index = head + index_from;

			if (count > size)
				throw new ArgumentOutOfRangeException("count too large");

			if (buffer_index >= capacity)
				buffer_index -= capacity;

			for (int i = 0; i < count; i++, buffer_index++)
			{
				if (buffer_index == capacity)
					buffer_index = 0;
				array[i] = buffer[buffer_index];
			}
		}

		public T PeekNewest()
		{
			if(size==0)
				throw new InvalidOperationException("Empty");
			if (tail == 0)
				return buffer[capacity - 1];

			return buffer[tail - 1];
		}

		public T PeekOldest()
		{
			if(size==0)
				throw new InvalidOperationException("Empty");

			return buffer[head];
		}

		/// <summary>
		/// Returns logical index - as if circular buffer actual content was index from 0 to size-1
		/// </summary>
		/// <returns>The search.</returns>
		/// <param name="value">Value.</param>
		public int BinarySearch(T value)
		{
			IComparer<T> comparer = Comparer<T>.Default;

			int lower = 0;
			int upper = size - 1;

			while (lower <= upper)
			{
				int middle = lower + (upper - lower) / 2;
				int comparisonResult = comparer.Compare(value, this[middle]);
					
				if (comparisonResult < 0)
					upper = middle - 1;
				else if (comparisonResult > 0)
					lower = middle + 1;
				else
					return middle;
			}

			return ~lower;
		}

		#endregion

        [NonSerialized()]
        private object syncRoot;

        public CircularBuffer(int capacity)
            : this(capacity, false)
        {
        }

        public CircularBuffer(int capacity, bool allowOverflow)
        {
            if (capacity < 0)
                throw new ArgumentException("capacity 0 is illegal");

            this.capacity = capacity;
            size = 0;
            head = 0;
            tail = 0;
            buffer = new T[capacity];
            AllowOverflow = allowOverflow;
        }

        public bool AllowOverflow
        {
            get;
            set;
        }

        public int Capacity
        {
            get { return capacity; }
            set
            {
                if (value == capacity)
                    return;

                if (value < size)
                    throw new ArgumentOutOfRangeException("value capacity is too small");

                var dst = new T[value];
                if (size > 0)
                    CopyTo(dst);
                buffer = dst;

                capacity = value;
            }
        }

        public int Size
        {
            get { return size; }
        }

        public bool Contains(T item)
        {
            int bufferIndex = head;
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < size; i++, bufferIndex++)
            {
                if (bufferIndex == capacity)
                    bufferIndex = 0;

                if (item == null && buffer[bufferIndex] == null)
                    return true;
                else if ((buffer[bufferIndex] != null) &&
                    comparer.Equals(buffer[bufferIndex], item))
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            size = 0;
            head = 0;
            tail = 0;
        }

        public int Put(T[] src)
        {
            return Put(src, 0, src.Length);
        }

        public int Put(T[] src, int offset, int count)
        {
            if (!AllowOverflow &&  count > capacity - size)
                throw new InvalidOperationException("Buffer overflow");

            int srcIndex = offset;
            for (int i = 0; i < count; i++, tail++, srcIndex++)
            {
                if (tail == capacity)
                    tail = 0;
                buffer[tail] = src[srcIndex];
            }
            size = Math.Min(size + count, capacity);
            return count;
        }

		//unoptimal temporary implementation
		public int Put(CircularBuffer<T> src)
		{
			T[] srcArray = src.ToArray ();
			Put (srcArray);
			return src.Size;
		}


        public void Put(T item)
        {
            if (!AllowOverflow && size == capacity)
                throw new InvalidOperationException("Buffer overflow");

            buffer[tail] = item;
		
			if (++tail == capacity)
			{
				tail = 0;
			}
			if (++size > capacity)
			{
				size = capacity;
				if (++head == capacity)
					head = 0;
			}
        }
			
        public void Skip(int count)
        {
            head += count;
            if (head >= capacity)
                head -= capacity;
        }

        public T[] Get(int count)
        {
            var dst = new T[count];
            Get(dst);
            return dst;
        }

        public int Get(T[] dst)
        {
            return Get(dst, 0, dst.Length);
        }

        public int Get(T[] dst, int offset, int count)
        {
            int realCount = Math.Min(count, size);
            int dstIndex = offset;
            for (int i = 0; i < realCount; i++, head++, dstIndex++)
            {
                if (head == capacity)
                    head = 0;
                dst[dstIndex] = buffer[head];
            }
            size -= realCount;
            return realCount;
        }

        public T Get()
        {
            if (size == 0)
                throw new InvalidOperationException("Empty");

            var item = buffer[head];
            if (++head == capacity)
                head = 0;
            size--;
            return item;
        }

        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(0, array, arrayIndex, size);
        }
			
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (count > size)
                throw new ArgumentOutOfRangeException("count too large");

            int bufferIndex = head;
            for (int i = 0; i < count; i++, bufferIndex++, arrayIndex++)
            {
                if (bufferIndex == capacity)
                    bufferIndex = 0;
                array[arrayIndex] = buffer[bufferIndex];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            int bufferIndex = head;
            for (int i = 0; i < size; i++, bufferIndex++)
            {
                if (bufferIndex == capacity)
                    bufferIndex = 0;

                yield return buffer[bufferIndex];
            }
        }

        public T[] GetBuffer()
        {
            return buffer;
        }

        public T[] ToArray()
        {
            var dst = new T[size];
            CopyTo(dst);
            return dst;
        }

        #region ICollection<T> Members

        int ICollection<T>.Count
        {
            get { return Size; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<T>.Add(T item)
        {
            Put(item);
        }

        bool ICollection<T>.Remove(T item)
        {
            if (size == 0)
                return false;

            Get();
            return true;
        }


        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region ICollection Members

        int ICollection.Count
        {
            get { return Size; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            CopyTo((T[])array, arrayIndex);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        #endregion
    }
}
