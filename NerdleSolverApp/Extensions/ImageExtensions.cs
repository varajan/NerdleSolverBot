using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NerdleSolverApp.Extensions;

internal static class ImageExtensions
{
    public static Image<Rgba32> GetBlackAndWhite(this Image<Rgba32> image, ColorType? color = null)
    {
        var result = image.Clone();
        result.Mutate(ctx => ctx.BinaryThreshold(0.5f));
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
        var threshold = 0.95;
        var i = 0;

        for (; i < image.Height; i++)
        {
            if (!image.IsMostlyColor(ColorType.Black, i, threshold: threshold)) break;
        }

        image = image.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(0, i, image.Width, image.Height - i)));


        return image;
    }

    public static Image<Rgba32> Normilized(this Image<Rgba32> image)
    {
        var firstNotWhiteColumn = image.FirstWithoutColor(ColorType.White, row: false, threshold: 0.95);
        var firstNotWhiteRow = image.FirstWithoutColor(ColorType.White, threshold: 0.95);
        //var firstNotWhiteColumn = FirstNonWhiteColumn(image);
        //var firstNotWhiteRow = FirstNonWhiteRow(image);
        firstNotWhiteColumn = firstNotWhiteColumn < 5 ? 5 : firstNotWhiteColumn;
        firstNotWhiteRow = firstNotWhiteRow < 5 ? 5 : firstNotWhiteRow;

        var result = image.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(
                firstNotWhiteColumn,
                firstNotWhiteRow,
                image.Width - firstNotWhiteColumn,
                image.Height - firstNotWhiteRow)));

        var width = result.FirstWithColor(ColorType.White, row: false, threshold: 0.95);
        var height = result.FirstWithColor(ColorType.White, threshold: 0.95);
        //var width = FirstWhiteColumn(result);
        //var height = FirstWhiteRow(result);
        width = width <= 0 ? result.Width : width;
        height = height <= 0 ? result.Height : height;

        result = result.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(0, 0, width, height)));

        return result;
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
