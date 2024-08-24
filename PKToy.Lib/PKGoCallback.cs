
using static PK.UNCLASSED;

namespace PKToy;

public unsafe class PKGoCallback:IDisposable
{
    const int CONTIN = (int)graphics_ifails_t.CONTIN;
    const int ABORT = (int)graphics_ifails_t.ABORT;
    private bool disposedValue;

    public PKGoCallback()
    {
        Frustrum.RegGoCallback(this);
    }

    public void GOSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        *ifail = CONTIN;
    }

    public void GOOpenSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {

        *ifail = CONTIN;
    }

    public void GOCloseSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        *ifail = CONTIN;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            Frustrum.UnRegGoCallback();
            disposedValue = true;
        }
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~PKGoCallback()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}