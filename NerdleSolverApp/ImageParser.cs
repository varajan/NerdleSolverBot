using NerdleSolverApp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NerdleSolverApp;

public class ImageParser(Stream stream)
{
    private Image<Rgba32> ImageToParse = Image.Load<Rgba32>(stream);
    private readonly string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
    private readonly string[] operations = { "+", "-", "*", "/", "=" };

    private static string GetKeyboardFileName(string key) =>
        key switch
        {
            "+" => "key_plus.png",
            "-" => "key_minus.png",
            "*" => "key_multiply.png",
            "/" => "key_divide.png",
            "=" => "key_equals.png",
            _ => $"key_{key}.png",
        };

    private IEnumerable<CellInfo[]> ExtractTableData(Image<Rgba32> tableImage)
    {
        tableImage.SaveAsPng("table.png");
        var rowHeight = GetRowHeight(tableImage);

        for (var i = 0; i < 10; i++)
        {
            var result = ExtractRowData(tableImage, i, rowHeight);
            if (result.All(x => x.Color == ColorType.White)) break;

            yield return result;
        }
    }

    private CellInfo[] ExtractRowData(Image<Rgba32> tableImage, int row, int rowHeight)
    {
        var delta = 15;
        var result = new CellInfo[8];
        var cellWidth = tableImage.Width / result.Length;

        for (var i = 0; i < result.Length; i++)
        {
            var cellImage = tableImage.Clone((IImageProcessingContext ctx) =>
                ctx.Crop(new Rectangle((i * cellWidth) + delta, (row * rowHeight) + delta, cellWidth - (2 * delta), rowHeight - (2 * delta))));
            var color = cellImage.GetColor();
            var text = color == ColorType.White ? "?" : GetCellText(cellImage);

            result[i] = new CellInfo(text, color);
        }

        return result;
    }

    private string GetCellText(Image<Rgba32> cellImage)
    {
        var color = cellImage.GetColor();
        cellImage.SaveAsPng("cellImage.png");
        var key = cellImage.GetBlackAndWhite(color).Normilized();
        var allKeys = keys.Concat(operations);
        key.Mutate(x => x.Resize(30, 35));

        var result = new List<(string key, double difference)>();

        foreach (var k in allKeys)
        {
            var keyImage = Image.Load<Rgba32>(GetKeyboardFileName(k));
            var difference = CalculateDifference(keyImage, key);

            result.Add((k, difference));
        }

        // 24x35 - cell
        // 25x35
        key.SaveAsPng("cell-as-key.png");

        var xxx = result.OrderBy(x => x.difference).First().key;

        return result.OrderBy(x => x.difference).First().key;
    }

    private static double CalculateDifference(Image<Rgba32> imageA, Image<Rgba32> imageB)
    {
        int difference = 0;
        int width = Math.Min(imageA.Width, imageB.Width);
        int height = Math.Min(imageA.Height, imageB.Height);


        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (imageA[x, y].GetColor() != imageB[x, y].GetColor())
                    difference++;
            }

        return (double)difference / (width * height);
    }

    private static int GetRowHeight(Image<Rgba32> tableImage)
    {
        var height = 0;

        for (; height < tableImage.Height; height++)
        {
            if (tableImage.IsMostlyColor(ColorType.White, height, threshold: 0.9))
                break;
        }

        var nextPossibleHeight = FirstTableRow(tableImage, 0.6, height);

        return nextPossibleHeight == 0 ? height : nextPossibleHeight;
    }

    private static IEnumerable<CellInfo> ExtractButtons(Image<Rgba32> keyboardImage, string[] buttonLabels, int row)
    {
        var cellWidth = keyboardImage.Width / 10;
        var cellHeight = keyboardImage.Height / 2;
        var delta = 15;

        for (var i = 0; i < buttonLabels.Length; i++)
        {
            var cellImage = keyboardImage.Clone((IImageProcessingContext ctx) =>
                ctx.Crop(new Rectangle((i * cellWidth) + delta, (row * cellHeight) + delta, cellWidth - (2 * delta), cellHeight - (2 * delta))));
            var color = cellImage.GetColor();

            cellImage.SaveAsPng("cellImage.png");
            var button = cellImage.GetBlackAndWhite(color).Normilized();

            button.Mutate(x => x.Resize(30, 35));
            button.SaveAsPng(GetKeyboardFileName(buttonLabels[i]));

            yield return new CellInfo(buttonLabels[i], color);
        }
    }

    private (Image<Rgba32> image, Rectangle size) FindKeyboard()
    {
        double grayThreshold = 0.9;
        //double grayThreshold = 0.8;

        int keyboardTop = ImageToParse.FirstWithColor(ColorType.Gray, threshold: grayThreshold);
        int keyboardBottom = ImageToParse.LastWithColor(ColorType.Gray, threshold: grayThreshold);
        var image = ImageToParse.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(
                0,
                keyboardTop,
                ImageToParse.Width,
                keyboardBottom - keyboardTop)));

        int keyboardLeft = image.FirstWithoutColor(ColorType.White, row: false, threshold: 0.95);
        int keyboardRight = keyboardLeft;
        //int keyboardRight = 0;
        image = image.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(
                keyboardLeft,
                0,
                image.Width - keyboardLeft - keyboardRight,
                image.Height)));

        var size = new Rectangle(keyboardLeft, keyboardTop, image.Width, image.Height);
        return (image, size);
    }

    private Image<Rgba32> FindTable(Rectangle keyboardSize)
    {
        double threshold = 0.60;

        int tableTop = FirstTableRow(ImageToParse, threshold);
        var cropped = ImageToParse.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(
                keyboardSize.Left,
                tableTop,
                keyboardSize.Width,
                keyboardSize.Top - tableTop)));

        return cropped;
    }

    private static int FirstTableRow(Image<Rgba32> image, double threshold, int startRow = 0)
    {
        for (int y = startRow; y < image.Height; y++)
        {
            if (image.IsMostlyColors([ColorType.Green, ColorType.Purple, ColorType.Black], y, threshold: threshold))
                return y;
        }
        return 0;
    }

    private string GetPattern(List<CellInfo[]> tableInfo)
    {
        if (tableInfo.Count == 0) return ".*";

        var patternLength = tableInfo[0].Length;
        string pattern = "";

        for (int i = 0; i < patternLength; i++)
        {
            var green = tableInfo.Select(row => row[i])
                .FirstOrDefault(cell => cell.Color == ColorType.Green && cell.Text.Length == 1);
            if (green is not null)
            {
                var key = green.Text;
                pattern += operations.Contains(key) && key != "=" ? $"\\{key}" : key;
                continue;
            }

            var notInPlace = tableInfo
                .Select(row => row[i])
                .Where(cell => cell.Color is ColorType.Purple or ColorType.Black)
                .Where(cell => cell.Text.Length == 1)
                .ToList();
            if (notInPlace.Any())
            {
                pattern += $"[^{string.Join("", notInPlace.Select(x => x.Text))}]";
                continue;
            }

            pattern += ".";
        }

        return pattern;
    }

    private Image<Rgba32> SkipHeader(double threshold = 0.6)
    {
        int headerHeight = 0;
        for (; headerHeight < ImageToParse.Height; headerHeight++)
        {
            if (!ImageToParse.IsMostlyColors([ColorType.White, ColorType.Gray, ColorType.Black], headerHeight, threshold: 0.9))
                //if (!ImageToParse.IsMostlyColor(ColorType.Black, headerHeight, threshold: 0.8))
                break;
        }

        for (; headerHeight < ImageToParse.Height; headerHeight++)
        {
            if (ImageToParse.IsMostlyColors([ColorType.Green, ColorType.Purple, ColorType.Black], headerHeight, threshold: threshold))
                break;
        }

        var cropped = ImageToParse.Clone((IImageProcessingContext ctx) =>
            ctx.Crop(new Rectangle(0, headerHeight, ImageToParse.Width, ImageToParse.Height - headerHeight)));
        return cropped;
    }

    public (string expected, string unexpected, string pattern) Parse()
    {
        ImageToParse.SaveAsPng("original.png");
        ImageToParse = SkipHeader();
        ImageToParse.SaveAsPng("SkipHeader1.png");
        ImageToParse = SkipHeader();
        ImageToParse.SaveAsPng("SkipHeader2.png");

        var keyboard = FindKeyboard();
        var table = FindTable(keyboard.size);

        table.SaveAsPng("table.png");
        keyboard.image.SaveAsPng("keyboard.png");

        var keysInfo = ExtractButtons(keyboard.image, keys, 0).Union(ExtractButtons(keyboard.image, operations, 1)).ToList();
        var tableInfo = ExtractTableData(table).ToList();

        var expected = keysInfo.Where(x => x.Color is ColorType.Green or ColorType.Purple).Select(x => x.Text).ToList();
        var unexpected = keysInfo.Where(x => x.Color is ColorType.Black).Select(x => x.Text).ToList();
        var white = keysInfo.Where(x => x.Color is ColorType.White).Select(x => x.Text).ToList();

        return (
            expected: string.Join("", expected),
            unexpected: expected.Count == 8 ? string.Join("", white) : string.Join("", unexpected),
            pattern: GetPattern(tableInfo));
    }
}
