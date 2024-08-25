
namespace PKToy.Lib;

public unsafe ref struct PKScopeArray<T> where T : unmanaged
{
    public int size;

    public T* data;

    public ref T this[int index] => ref data[index];

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

public unsafe ref struct PKFacetResult
{
    public PK.TOPOL.facet_r_t data;

    public unsafe void Dispose()
    {
        fixed (PK.TOPOL.facet_r_t* ptr = &data)
        {
            PKErrorCheck err = PK.TOPOL.facet_r_f(ptr);
        }
    }
}
