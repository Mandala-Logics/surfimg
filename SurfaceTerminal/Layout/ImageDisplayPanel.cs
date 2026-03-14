using System;
using System.IO;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public sealed class ImageDisplayPanel : SurfacePanel, IDisposable
    {
        public override bool CanBeSelected => false;

        public string? ImagePath { get; private set; }
        public bool UseColour { get; set; } = true;
        public bool UseDensityRamp { get; set; } = false;
        public bool Dither { get; set; } = false;

        public string DensityRamp { get; set; } = "@%#*+=-:. ";

        private Image<Rgba32>? _image;
        private ConsoleChar[,]? _cachedFrame;
        private int _lastRenderWidth = -1;
        private int _lastRenderHeight = -1;

        public ImageDisplayPanel() { }

        public ImageDisplayPanel(string imagePath)
        {
            Load(imagePath);
        }

        public void Load(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Image path cannot be null or whitespace.", nameof(imagePath));

            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Image file not found.", imagePath);

            DisposeImage();

            _image = Image.Load<Rgba32>(imagePath);
            ImagePath = imagePath;
            InvalidateCache();
        }

        public void Load(Image<Rgba32> image)
        {
            if (image is null) throw new ArgumentNullException(nameof(image));

            DisposeImage();

            _image = image.Clone();
            ImagePath = null;
            InvalidateCache();
        }

        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            if (_image is null)
            {
                surface.Fill(ConsoleChar.WhiteSpace);
                return;
            }

            if (surface.Width <= 0 || surface.Height <= 0)
                return;

            if (_cachedFrame is null
                || _lastRenderWidth != surface.Width
                || _lastRenderHeight != surface.Height)
            {
                _cachedFrame = BuildFrame(surface.Width, surface.Height);
                _lastRenderWidth = surface.Width;
                _lastRenderHeight = surface.Height;
            }

            for (int y = 0; y < surface.Height; y++)
            {
                for (int x = 0; x < surface.Width; x++)
                {
                    surface[x, y] = _cachedFrame[x, y];
                }
            }
        }

        private ConsoleChar[,] BuildFrame(int targetWidth, int targetHeight)
        {
            var output = new ConsoleChar[targetWidth, targetHeight];
            int sampleHeight = Math.Max(1, targetHeight * 2);

            using var scaled = _image!.Clone(ctx =>
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(targetWidth, sampleHeight),
                    Mode = ResizeMode.Stretch,
                    Sampler = KnownResamplers.Bicubic
                });
            });

            for (int y = 0; y < targetHeight; y++)
            {
                int topY = y * 2;
                int bottomY = Math.Min(topY + 1, scaled.Height - 1);

                var topRow = scaled.DangerousGetPixelRowMemory(topY).Span;
                var bottomRow = scaled.DangerousGetPixelRowMemory(bottomY).Span;

                for (int x = 0; x < targetWidth; x++)
                {
                    var top = topRow[x];
                    var bottom = bottomRow[x];

                    output[x, y] = UseColour
                        ? BuildColourCell(top, bottom)
                        : BuildMonoCell(top, bottom);
                }
            }

            return output;
        }

        private ConsoleChar BuildColourCell(Rgba32 top, Rgba32 bottom)
        {
            var fg = ToConsoleColor(top);
            var bg = ToConsoleColor(bottom);

            if (UseDensityRamp)
            {
                int avg = (Luma(top) + Luma(bottom)) / 2;
                char c = MapBrightnessToChar(avg);
                return new ConsoleTextChar(c, new ConsoleDecoration(fg, bg));
            }

            return new ConsoleTextChar('▀', new ConsoleDecoration(fg, bg));
        }

        private ConsoleChar BuildMonoCell(Rgba32 top, Rgba32 bottom)
        {
            int avg = (Luma(top) + Luma(bottom)) / 2;

            if (Dither)
            {
                avg = avg >= 128
                    ? Math.Min(255, avg + 20)
                    : Math.Max(0, avg - 20);
            }

            char c = MapBrightnessToChar(avg);
            return new ConsoleTextChar(c, default);
        }

        private char MapBrightnessToChar(int brightness)
        {
            if (DensityRamp.Length == 0) return ' ';

            int idx = (int)Math.Round((brightness / 255.0) * (DensityRamp.Length - 1));
            idx = Math.Clamp(idx, 0, DensityRamp.Length - 1);
            return DensityRamp[idx];
        }

        private static int Luma(Rgba32 c)
        {
            return (int)Math.Round(0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B);
        }

        private static ConsoleColor ToConsoleColor(Rgba32 c)
        {
            var palette = new (ConsoleColor cc, Rgba32 rgb)[]
            {
                (ConsoleColor.Black,       new Rgba32(0, 0, 0)),
                (ConsoleColor.DarkBlue,    new Rgba32(0, 0, 128)),
                (ConsoleColor.DarkGreen,   new Rgba32(0, 128, 0)),
                (ConsoleColor.DarkCyan,    new Rgba32(0, 128, 128)),
                (ConsoleColor.DarkRed,     new Rgba32(128, 0, 0)),
                (ConsoleColor.DarkMagenta, new Rgba32(128, 0, 128)),
                (ConsoleColor.DarkYellow,  new Rgba32(128, 128, 0)),
                (ConsoleColor.Gray,        new Rgba32(192, 192, 192)),
                (ConsoleColor.DarkGray,    new Rgba32(128, 128, 128)),
                (ConsoleColor.Blue,        new Rgba32(0, 0, 255)),
                (ConsoleColor.Green,       new Rgba32(0, 255, 0)),
                (ConsoleColor.Cyan,        new Rgba32(0, 255, 255)),
                (ConsoleColor.Red,         new Rgba32(255, 0, 0)),
                (ConsoleColor.Magenta,     new Rgba32(255, 0, 255)),
                (ConsoleColor.Yellow,      new Rgba32(255, 255, 0)),
                (ConsoleColor.White,       new Rgba32(255, 255, 255))
            };

            double bestDistance = double.MaxValue;
            ConsoleColor best = ConsoleColor.White;

            foreach (var p in palette)
            {
                int dr = c.R - p.rgb.R;
                int dg = c.G - p.rgb.G;
                int db = c.B - p.rgb.B;

                double dist = dr * dr + dg * dg + db * db;
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = p.cc;
                }
            }

            return best;
        }

        private void InvalidateCache()
        {
            _cachedFrame = null;
            _lastRenderWidth = -1;
            _lastRenderHeight = -1;
        }

        protected override void OnDeselected() { }
        protected override void OnSelected() { }
        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo) { }
        protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState) => true;

        private void DisposeImage()
        {
            _image?.Dispose();
            _image = null;
        }

        public void Dispose()
        {
            DisposeImage();
        }
    }
}
