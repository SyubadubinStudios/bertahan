using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ThreeNet;

namespace Bertahan.Core;

/// <summary>Saves the last rendered 3D frame (optionally with the UI drawn on top) as a PNG.</summary>
public static class Screenshot
{
    public static unsafe void Save(Renderer renderer, string path, params Visual[] overlays)
    {
        int width = renderer.Width;
        int height = renderer.Height;
        byte[] pixels = renderer.ReadPixels();
        // The renderer leaves alpha at zero; force it opaque so viewers show the image.
        for (int i = 3; i < pixels.Length; i += 4)
        {
            pixels[i] = 255;
        }

        using WriteableBitmap frame = new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (ILockedFramebuffer buffer = frame.Lock())
        {
            int rowBytes = width * 4;
            for (int y = 0; y < height; y++)
            {
                new Span<byte>(pixels, y * rowBytes, rowBytes)
                    .CopyTo(new Span<byte>((void*)(buffer.Address + (nint)(y * buffer.RowBytes)), rowBytes));
            }
        }

        if (overlays.Length == 0)
        {
            frame.Save(path);
            return;
        }

        // Composite: the 3D frame scaled to the window, then each UI layer rendered on top.
        PixelSize target = new(width, height);
        using RenderTargetBitmap result = new(target, new Vector(96, 96));
        Rect rect = new(0, 0, width, height);
        List<RenderTargetBitmap> layers = [];
        foreach (Visual overlay in overlays)
        {
            // render at the logical size, then stretch: RenderTargetBitmap with a DPI scale misplaces transformed children
            PixelSize logical = new(Math.Max(1, (int)overlay.Bounds.Width), Math.Max(1, (int)overlay.Bounds.Height));
            RenderTargetBitmap ui = new(logical, new Vector(96, 96));
            ui.Render(overlay);
            layers.Add(ui);
        }

        using (var context = result.CreateDrawingContext())
        {
            context.DrawImage(frame, rect);
            foreach (RenderTargetBitmap ui in layers)
            {
                context.DrawImage(ui, rect);
            }
        }

        foreach (RenderTargetBitmap ui in layers)
        {
            ui.Dispose();
        }

        result.Save(path);
    }
}
