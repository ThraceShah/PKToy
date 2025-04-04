namespace PKToy.Exchange;
internal unsafe ref struct PKScopeArray<T> where T : unmanaged
{
    public int size;

    public T* data;

    public ref T this[int index] => ref data[index];

    public readonly Span<T> Span => new(data, size);

    public readonly ReadOnlySpan<T> ReadOnlySpan => new(data, size);

    public readonly Span<T> this[Range range]
    {
        get
        {
            var (start, length) = range.GetOffsetAndLength(size);
            return new Span<T>(data + start, length);
        }
    }

    public readonly Enumerator GetEnumerator()
    {
        return new Enumerator(data, size);
    }


    public void Dispose()
    {
        if (data != null)
        {
            var err = PK.MEMORY.free(data);
        }
        size = 0;
        data = null;
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


internal unsafe ref partial struct PKScopeData<T>(delegate*<T*, PK.ERROR.code_t> freeFunc) where T : unmanaged
{

    public T data;

    public void Dispose()
    {
        if (freeFunc != null)
        {
            fixed (T* ptr = &data)
            {
                freeFunc(ptr);
            }
        }
    }
}
