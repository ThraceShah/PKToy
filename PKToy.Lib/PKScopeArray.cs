
namespace PKToy.Lib;

public unsafe ref struct PKScopeArray<T> where T : unmanaged
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
            var err = PK_MEMORY_free(data);
        }
        data = null;
    }
}

public unsafe ref struct PK_acetResult
{
    public PK_TOPOL_facet_r_t data;

    public unsafe void Dispose()
    {
        fixed (PK_TOPOL_facet_r_t* ptr = &data)
        {
            PK_TOPOL_facet_r_f(ptr);
        }
    }
}
