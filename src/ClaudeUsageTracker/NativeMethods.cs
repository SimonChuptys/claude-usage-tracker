using System.Runtime.InteropServices;

namespace ClaudeUsageTracker;

internal static partial class NativeMethods
{
    /// <summary>
    /// Frees an icon handle created with <c>Bitmap.GetHicon</c>. Required to
    /// avoid leaking GDI handles when regenerating the tray icon repeatedly.
    /// </summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(IntPtr handle);
}
