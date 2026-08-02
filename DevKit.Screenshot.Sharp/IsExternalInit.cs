#if !NET5_0_OR_GREATER
using System.ComponentModel;

namespace System.Runtime.CompilerServices;

// Enables init accessors and records on netstandard2.0.
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit;
#endif
