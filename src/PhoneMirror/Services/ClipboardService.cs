using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PhoneMirror.Services;

/// <summary>
/// Provides clipboard operations with proper image support on Windows.
/// </summary>
public static class ClipboardService
{
    // Windows clipboard formats
    private const uint CF_BITMAP = 2;
    private const uint CF_DIB = 8;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint GMEM_ZEROINIT = 0x0040;

    /// <summary>
    /// Copies a PNG image to the Windows clipboard so it can be pasted with Ctrl+V.
    /// </summary>
    /// <param name="pngData">The PNG image data.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static async Task<bool> CopyImageToClipboardAsync(byte[] pngData)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        return await Task.Run(() =>
        {
            try
            {
                // Convert PNG to DIB (Device Independent Bitmap) format
                using var ms = new MemoryStream(pngData);
                using var bitmap = new System.Drawing.Bitmap(ms);

                // Get the DIB data
                var dibData = GetDibFromBitmap(bitmap);
                if (dibData == null || dibData.Length == 0)
                {
                    return false;
                }

                // Open clipboard
                if (!OpenClipboard(IntPtr.Zero))
                {
                    return false;
                }

                try
                {
                    // Empty clipboard
                    EmptyClipboard();

                    // Allocate global memory for the DIB
                    var hMem = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, (UIntPtr)dibData.Length);
                    if (hMem == IntPtr.Zero)
                    {
                        return false;
                    }

                    // Lock memory and copy data
                    var ptr = GlobalLock(hMem);
                    if (ptr == IntPtr.Zero)
                    {
                        GlobalFree(hMem);
                        return false;
                    }

                    Marshal.Copy(dibData, 0, ptr, dibData.Length);
                    GlobalUnlock(hMem);

                    // Set clipboard data
                    if (SetClipboardData(CF_DIB, hMem) == IntPtr.Zero)
                    {
                        GlobalFree(hMem);
                        return false;
                    }

                    // Note: Don't free hMem after successful SetClipboardData - clipboard owns it now
                    return true;
                }
                finally
                {
                    CloseClipboard();
                }
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Converts a System.Drawing.Bitmap to DIB format bytes.
    /// </summary>
    private static byte[]? GetDibFromBitmap(System.Drawing.Bitmap bitmap)
    {
        try
        {
            var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                var stride = bitmapData.Stride;
                var height = bitmap.Height;
                var width = bitmap.Width;

                // Calculate DIB size (BITMAPINFOHEADER + pixel data)
                // Note: DIB stores rows bottom-to-top
                var headerSize = 40; // BITMAPINFOHEADER size
                var pixelDataSize = Math.Abs(stride) * height;
                var dibData = new byte[headerSize + pixelDataSize];

                // Write BITMAPINFOHEADER
                using var ms = new MemoryStream(dibData);
                using var writer = new BinaryWriter(ms);

                writer.Write(40);           // biSize
                writer.Write(width);        // biWidth
                writer.Write(height);       // biHeight (positive = bottom-up)
                writer.Write((short)1);     // biPlanes
                writer.Write((short)32);    // biBitCount
                writer.Write(0);            // biCompression (BI_RGB)
                writer.Write(pixelDataSize); // biSizeImage
                writer.Write(0);            // biXPelsPerMeter
                writer.Write(0);            // biYPelsPerMeter
                writer.Write(0);            // biClrUsed
                writer.Write(0);            // biClrImportant

                // Copy pixel data (flip vertically for DIB format)
                var sourcePtr = bitmapData.Scan0;
                var rowSize = Math.Abs(stride);

                for (int y = height - 1; y >= 0; y--)
                {
                    var sourceRow = IntPtr.Add(sourcePtr, y * stride);
                    var rowData = new byte[rowSize];
                    Marshal.Copy(sourceRow, rowData, 0, rowSize);
                    ms.Write(rowData, 0, rowSize);
                }

                return dibData;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
        catch
        {
            return null;
        }
    }
}
