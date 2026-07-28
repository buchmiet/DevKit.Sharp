#if !NET9_0_OR_GREATER
namespace System.Threading;

// Minimal polyfill so `lock (System.Threading.Lock)` statements compile on TFMs
// before .NET 9. The compiler lowers them to EnterScope()/Dispose(), which here
// degrades to plain Monitor semantics.
internal sealed class Lock
{
#pragma warning disable CS9216 // intentional: the polyfill itself implements the monitor
    public Scope EnterScope()
    {
        Monitor.Enter(this);
        return new Scope(this);
    }

    public ref struct Scope(Lock owner)
    {
        public void Dispose() => Monitor.Exit(owner);
    }
#pragma warning restore CS9216
}
#endif
