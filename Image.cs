using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NerdleSolver
{
    internal record CellInfo(string Text, ColorType Color);

    internal enum ColorType
    {
        White,
        Green,
        Purple,
        Black,
    }

    internal class ImageParser(Stream stream)
    {
        private readonly Image<Rgba32> ImageToParse = Image.Load<Rgba32>(stream);
        private readonly string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private readonly string[] operations = { "+", "-", "*", "/" };

        private static IEnumerable<CellInfo> ExtractButtons(Image<Rgba32> keyboardImage, string[] buttonLabels, int row)
        {
            var cellWidth = keyboardImage.Width / 10;
            var cellHeight = keyboardImage.Height / 2;

            for (var i = 0; i < buttonLabels.Length; i++)
            {
                var cellImage = keyboardImage.Clone((IImageProcessingContext ctx) =>
                    ctx.Crop(new Rectangle(i * cellWidth, row * cellHeight, cellWidth, cellHeight)));
                var color = GetColor(cellImage);
                yield return new CellInfo(buttonLabels[i], color);
            }
        }

        private static ColorType GetColor(Image<Rgba32> cellImage)
        {
            int threshold = 250;
            int white = 0;
            int green = 0;
            int purple = 0;
            int black = 0;

            for (var i = 0; i < cellImage.Width; i++)
            {
                for (var j = 0; j < cellImage.Height; j++)
                {
                    var pixel = cellImage[i, j];

                    if (IsWhite(pixel))
                    {
                        white++;
                        if (white > threshold) return ColorType.White;
                        continue;
                    }

                    if (IsGreen(pixel))
                    {
                        green++;
                        if (green > threshold) return ColorType.Green;
                        continue;
                    }

                    if (IsPurple(pixel))
                    {
                        purple++;
                        if (purple > threshold) return ColorType.Purple;
                        continue;
                    }

                    if (IsBlack(pixel))
                    {
                        black++;
                        if (black > threshold) return ColorType.Black;
                        continue;
                    }
                }
            }

            return ColorType.White;
        }

        private Image<Rgba32> FindKeyboard()
        {
            double grayThreshold = 0.95;

            int keyboardTop = FirstGrayRow(grayThreshold);
            int keyboardBottom = LastGrayRow(keyboardTop, grayThreshold);
            int keyboardLeft = FirstNonWhiteColumn();
            int keyboardRight = FirstWhiteColumn(keyboardLeft);

            var cropped = ImageToParse.Clone((IImageProcessingContext ctx) =>
                ctx.Crop(new Rectangle(
                    keyboardLeft,
                    keyboardTop,
                    keyboardRight - keyboardLeft,
                    keyboardBottom - keyboardTop)));
            return cropped;
        }

        private int FirstGrayRow(double threshold)
        {
            for (int y = 0; y < ImageToParse.Height; y++)
            {
                if (IsMostlyGrayRow(y, threshold))
                    return y;
            }
            return 0;
        }

        private int LastGrayRow(int startRow, double threshold)
        {
            for (int y = ImageToParse.Height - 1; y >= startRow; y--)
            {
                if (IsMostlyGrayRow(y, threshold))
                    return y;
            }
            return ImageToParse.Height - 1;
        }

        private bool IsMostlyGrayRow(int y, double threshold)
        {
            int grayPixels = 0;
            for (int x = 0; x < ImageToParse.Width; x++)
            {
                var pixel = ImageToParse[x, y];
                if (IsGray(pixel)) grayPixels++;
            }

            return (double)grayPixels / ImageToParse.Width >= threshold;
        }

        private int FirstNonWhiteColumn()
        {
            for (int x = 0; x < ImageToParse.Width; x++)
            {
                if (!IsFullyWhiteColumn(x))
                    return x;
            }
            return -1;
        }

        private int FirstWhiteColumn(int startColumn)
        {
            for (int x = startColumn; x < ImageToParse.Width; x++)
            {
                if (IsFullyWhiteColumn(x))
                    return x;
            }
            return -1;
        }

        private bool IsFullyWhiteColumn(int x)
        {
            for (int y = 0; y < ImageToParse.Height; y++)
            {
                var pixel = ImageToParse[x, y];
                if (!IsWhite(pixel)) return false;
            }

            return true;
        }

        // rgb(245, 246, 249)
        private static bool IsWhite(Rgba32 pixel) => pixel.R > 240 && pixel.G > 240 && pixel.B > 240;

        // rgb(223, 227, 238)
        private static bool IsGray(Rgba32 pixel) =>
            (pixel.R is > 220 and < 240) && (pixel.G is > 220 and < 240) && (pixel.B is > 220 and < 240);

        // rgb(127, 35, 99)
        // rgb(128, 4, 88)
        private static bool IsPurple(Rgba32 pixel) =>
            (pixel.R is > 125 and < 130) && pixel.G is < 40 && pixel.B is > 85 and < 100;

        // rgb(60, 136, 117)
        private static bool IsGreen(Rgba32 pixel) =>
            (pixel.R is > 55 and < 65) && (pixel.G is > 130 and < 140) && (pixel.B is > 110 and < 120);

        // rgb(22, 24, 3)
        // rgb(24, 25, 12)
        private static bool IsBlack(Rgba32 pixel) =>
            (pixel.R is > 20 and < 30) && (pixel.G is > 20 and < 30) && (pixel.B < 20);

        public (string expected, string unexpected, string pattern) Parse()
        {
            var keyboard = FindKeyboard();
            var keysInfo = ExtractButtons(keyboard, keys, 0).Union(ExtractButtons(keyboard, operations, 1)).ToList();

            var expected = keysInfo.Where(x => x.Color is ColorType.Green or ColorType.Purple).Select(x => x.Text).ToList();
            var unexpected = keysInfo.Where(x => x.Color is ColorType.Black).Select(x => x.Text).ToList();
            var white = keysInfo.Where(x => x.Color is ColorType.White).Select(x => x.Text).ToList();

            return (
                expected: string.Join("", expected),
                unexpected: expected.Count == 7 ? string.Join("", white) : string.Join("", unexpected),
                pattern: ".*");
        }
    }
}
