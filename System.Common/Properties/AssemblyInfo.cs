using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("System.Common.Tests")]

namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Reserved to be used by the compiler")]
    internal static class IsExternalInit
    {
    }
}