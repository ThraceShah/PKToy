
namespace PKToy;

public unsafe ref struct PKScopeArray<T> where T : unmanaged
{
    public int size;

    public T* data;

    public readonly Span<T> AsSpan => new(data, size);

    public readonly ReadOnlySpan<T> AsReadOnlySpan => new(data, size);

    public void Dispose()
    {
        if (data != null)
        {
            var err = PK.MEMORY.free(data);
        }
        data = null;
    }
}
