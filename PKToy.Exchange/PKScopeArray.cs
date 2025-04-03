namespace PKToy.Exchange;
internal unsafe ref struct PKScopeArray<T> where T : unmanaged
{
    public int size;

    public T* data;

    public ref T this[int index] => ref data[index];

    public readonly Span<T> Span => new(data, size);

    public readonly ReadOnlySpan<T> ReadOnlySpan => new(data, size);

    public void Dispose()
    {
        if (data != null)
        {
            var err = PK.MEMORY.free(data);
        }
        data = null;
    }
}
