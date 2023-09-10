using System;

namespace Slant.Entity;

public sealed class AmbientContextSuppressor : IDisposable
{
    private DbContextScope? _savedScope;
    private bool _disposed;

    public AmbientContextSuppressor()
    {
        _savedScope = DbContextScope.GetAmbientScope();

        // We're hiding the ambient scope, but keeping a reference
        // so we can restore it later.
        DbContextScope.RemoveAmbientScope();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_savedScope != null)
        {
            DbContextScope.SetAmbientScope(_savedScope);
            _savedScope = null;
        }

        _disposed = true;
    }
}