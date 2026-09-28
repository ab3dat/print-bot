using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrintBot.Helpers;

/// <summary>
/// Resolves the shell icon Windows Explorer associates with a given file extension
/// (e.g. the installed Adobe Reader / Word / Excel icon), so the queue shows the same
/// icon the user already recognizes instead of a generic document glyph.
/// </summary>
public static class FileIconHelper
{
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public nint hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern nint SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint hIcon);

    // Per-extension icons never change while the app is running, so cache them —
    // SHGetFileInfo is a fairly expensive shell call and the DataGrid re-queries per row.
    private static readonly ConcurrentDictionary<string, ImageSource?> _cache =
        new(System.StringComparer.OrdinalIgnoreCase);

    public static ImageSource? GetIconForExtension(string? extension)
    {
        var ext = extension?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
        return _cache.GetOrAdd(ext, ResolveIcon);
    }

    private static ImageSource? ResolveIcon(string ext)
    {
        var shinfo = new SHFILEINFO();
        // The file doesn't need to exist — SHGFI_USEFILEATTRIBUTES makes the shell resolve
        // the icon purely from the fake path's extension.
        var probePath = string.IsNullOrEmpty(ext) ? "file" : $"file.{ext}";

        var result = SHGetFileInfo(probePath, FILE_ATTRIBUTE_NORMAL, ref shinfo,
            (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);

        if (result == 0 || shinfo.hIcon == 0) return null;

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(shinfo.hIcon);
        }
    }
}
