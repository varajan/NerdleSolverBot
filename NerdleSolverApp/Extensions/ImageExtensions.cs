using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NerdleSolverApp.Extensions;

internal static class ImageExtensions
{
    public static Image<Rgba32> GetBlackAndWhite(this Image<Rgba32> image, ColorType? color = null)
    {
        var result = image.Clone();
        result.Mutate(ctx => ctx.BinaryThreshold(0.75f));
        return (color == ColorType.White) ? InvertColors(result) : result;
    }

    public static Image<Rgba32> InvertColors(this Image<Rgba32> image)
    {
        var result = image.Clone();

        result.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);

                for (int x = 0; x < row.Length; x++)
                {
                    ref var p = ref row[x];

                    int brightness = p.R + p.G + p.B;

                    p = brightness > 382
                        ? new Rgba32(0, 0, 0)
                        : new Rgba32(255, 255, 255);
                }
            }
        });

        return result;
    }

    public static Image<Rgba32> Trim(this Image<Rgba32> image)
    {
        var threshold = 1;

        int x0 = 0;
        for (; x0 < image.Width; x0++)
        {
            if (!image.IsMostlyColor(ColorType.Black, x0, row: false, threshold: threshold)) break;
        }

        int y0 = 0;
        for (; y0 < image.Height; y0++)
        {
            if (!image.IsMostlyColor(ColorType.Black, y0, row: true, threshold: threshold)) break;
        }

        int x1 = image.Width - 1;
        for (; x1 > x0; x1--)
        {
            if (!image.IsMostlyColor(ColorType.Black, x1, row: false, threshold: threshold)) break;
        }

        int y1 = image.Height - 1;
        for (; y1 > y0; y1--)
        {
            if (!image.IsMostlyColor(ColorType.Black, y1, row: true, threshold: threshold)) break;
        }

        image.SaveAsPng("image.png");

        image = image.Clone((IImageProcessingContext ctx) => ctx.Crop(new Rectangle(x0, y0, x1 - x0, y1 - y0)));

        return image;
    }

    public static Image<Rgba32> Normilized(this Image<Rgba32> image) =>
        image.CutWhiteEdges().CutCorners(ColorType.White, ColorType.Black).Trim();

    private static Image<Rgba32> CutWhiteEdges(this Image<Rgba32> image)
    {
        var firstNotWhiteColumn = image.FirstWithoutColor(ColorType.White, row: false, threshold: 0.95);
        var firstNotWhiteRow = image.FirstWithoutColor(ColorType.White, threshold: 0.95);
        firstNotWhiteColumn = firstNotWhiteColumn < 5 ? 5 : firstNotWhiteColumn;
        firstNotWhiteRow = firstNotWhiteRow < 5 ? 5 : firstNotWhiteRow;

        var width = image.Width - firstNotWhiteColumn;
        var height = image.Height - firstNotWhiteRow;

        image = image.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(firstNotWhiteColumn, firstNotWhiteRow, width, height)));

        width = image.FirstWithColor(ColorType.White, row: false, threshold: 0.95);
        height = image.FirstWithColor(ColorType.White, threshold: 0.95);
        width = width <= 0 ? image.Width : width;
        height = height <= 0 ? image.Height : height;

        image = image.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(0, 0, width, height)));

        return image;
    }

    private static Image<Rgba32> CutCorners(this Image<Rgba32> image, ColorType colorToDelete, ColorType colorToSet)
    {
        image = image.CutCorner(colorToDelete, colorToSet, 0, 0);
        image = image.CutCorner(colorToDelete, colorToSet, 0, image.Height - 1);
        image = image.CutCorner(colorToDelete, colorToSet, image.Width - 1, 0);
        image = image.CutCorner(colorToDelete, colorToSet, image.Width - 1, image.Height - 1);

        return image;
    }

    private static Image<Rgba32> CutCorner(this Image<Rgba32> image, ColorType colorToDelete, ColorType colorToSet, int x, int y)
    {
        var corner = image[x, y];
        var color = corner.GetColor();
        if (corner.GetColor() == colorToSet) return image;

        // calculate widht
        int width = 1;
        if (x == 0)
        {
            for (; width < image.Width; width++)
            {
                if (image[width, y].GetColor() != colorToDelete) break;
                width++;
            }
        }
        else
        {
            for (; width < image.Width; width++)
            {
                if (image[x - width, y].GetColor() != colorToDelete) break;
                width++;
            }
        }

        // calculate height
        int height = 1;
        if (y == 0)
        {
            for (; height < image.Height; height++)
            {
                if (image[y, height].GetColor() != colorToDelete) break;
                height++;
            }
        }
        else
        {
            for (; height < image.Height; height++)
            {
                if (image[x, y - height].GetColor() != colorToDelete) break;
                height++;
            }
        }

        var cutRectangle = (x, y) switch
        {
            (0, 0) => new Rectangle(width, height, image.Width - width, image.Height - height),
            (0, _) => new Rectangle(0, 0, image.Width - width, image.Height - height),
            (_, 0) => new Rectangle(0, height, image.Width, image.Height - height),
            (_, _) => new Rectangle(0, 0, image.Width - width, image.Height - height)
        };

        image.SaveAsPng("CutCorner.png");
        image = image.Clone((IImageProcessingContext ctx) => ctx.Crop(cutRectangle));
        return image;
    }

    public static int FirstWithoutColor(this Image<Rgba32> image, ColorType color, bool row = true, double threshold = 0.95)
    {
        int size = row ? image.Height : image.Width;
        for (int y = 0; y < size; y++)
        {
            if (!image.IsMostlyColor(color, y, row: row, threshold: threshold))
                return y;
        }

        return 0;
    }

    public static int FirstWithColor(this Image<Rgba32> image, ColorType color, bool row = true, double threshold = 0.95)
    {
        int size = row ? image.Height : image.Width;
        for (int y = 0; y < size; y++)
        {
            if (image.IsMostlyColor(color, y, row: row, threshold: threshold))
                return y;
        }

        return 0;
    }

    public static int LastWithColor(this Image<Rgba32> image, ColorType color, bool row = true, double threshold = 0.95)
    {
        int size = row ? image.Height : image.Width;
        for (int y = size - 1; y >= 0; y--)
        {
            if (image.IsMostlyColor(color, y, row: row, threshold: threshold))
                return y;
        }

        return 0;
    }

    public static ColorType GetColor(this Image<Rgba32> cellImage)
    {
        int threshold = cellImage.Width * cellImage.Height / 10;
        int white = 0;
        int green = 0;
        int purple = 0;
        int black = 0;

        for (var i = 0; i < cellImage.Width; i++)
        {
            for (var j = 0; j < cellImage.Height; j++)
            {
                var pixel = cellImage[i, j];

                if (pixel.IsWhite())
                {
                    white++;
                    if (white > threshold) return ColorType.White;
                    continue;
                }

                if (pixel.IsGreen())
                {
                    green++;
                    if (green > threshold) return ColorType.Green;
                    continue;
                }

                if (pixel.IsPurple())
                {
                    purple++;
                    if (purple > threshold) return ColorType.Purple;
                    continue;
                }

                if (pixel.IsBlack())
                {
                    black++;
                    if (black > threshold) return ColorType.Black;
                    continue;
                }
            }
        }

        return ColorType.White;
    }

    public static bool IsMostlyColor(this Image<Rgba32> image, ColorType color, int i, bool row = true, double threshold = 0.95)
    {
        int colorCount = 0;
        int size = row ? image.Width : image.Height;

        for (int x = 0; x < size; x++)
        {
            var pixel = row ? image[x, i] : image[i, x];
            if (pixel.GetColor() == color) colorCount++;
        }

        return (double)colorCount / size >= threshold;
    }

    public static bool IsMostlyColors(this Image<Rgba32> image, ColorType[] colors, int i, bool row = true, double threshold = 0.95)
    {
        int colorCount = 0;
        int size = row ? image.Width : image.Height;
        for (int x = 0; x < size; x++)
        {
            var pixel = row ? image[x, i] : image[i, x];
            if (colors.Contains(pixel.GetColor())) colorCount++;
        }

        return (double)colorCount / size >= threshold;
    }
}
