using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NativeCorLib;

public unsafe class UMArray<T>(int size) : IDisposable where T : unmanaged
{
    private T* _data = (T*)NativeMemory.Alloc((uint)(size * sizeof(T)));
    private int _count = size;
    private bool _isDisposed = false;

    public ref T this[int index]
    {
        get
        {
            Debug.Assert(index < _count);
            return ref _data[index];
        }
    }

    public Span<T> this[Range range]
    {
        get
        {
            var (start, length) = range.GetOffsetAndLength(_count);
            return new Span<T>(_data + start, length);
        }
    }


    public int Count => _count;


    public T* Data => _data;

    public UMArray() : this(0)
    {
    }


    public ref T At(int index)
    {
        return ref _data[index];
    }

    public Span<T> AsSpan()
    {
        return new Span<T>(_data, _count);
    }

    public Span<T> Slice(int start, int length)
    {
        return new Span<T>(_data + start, length);
    }

    public T[] ToArray()
    {
        var array = new T[_count];
        for (int i = 0; i < _count; i++)
        {
            array[i] = _data[i];
        }
        return array;
    }

    public static implicit operator Span<T>(UMArray<T> list) => list.AsSpan();

    public static implicit operator T*(UMArray<T> list) => list._data;

    public static implicit operator T[](UMArray<T> list) => list.ToArray();


    public Enumerator GetEnumerator()
    {
        return new Enumerator(_data, _count);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // Free managed resources
        }

        // Free unmanaged resources
        NativeMemory.Free(_data);
        _data = null;
        _count = 0;

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~UMArray()
    {
        Dispose(false);
    }

    public ref struct Enumerator(T* data, int count)
    {
        private readonly T* _data = data;
        private readonly int _count = count;
        private int index = -1;

        public bool MoveNext()
        {
            index++;
            return index < _count;
        }

        public ref T Current
        {
            get
            {
                return ref _data[index];
            }
        }
    }

}
