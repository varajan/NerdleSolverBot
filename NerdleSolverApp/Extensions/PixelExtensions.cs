using SixLabors.ImageSharp.PixelFormats;

namespace NerdleSolverApp.Extensions;

internal static class PixelExtensions
{
    public static ColorType GetColor(this Rgba32 pixel)
    {
        if (pixel.IsWhite()) return ColorType.White;
        if (pixel.IsGreen()) return ColorType.Green;
        if (pixel.IsPurple()) return ColorType.Purple;
        if (pixel.IsBlack()) return ColorType.Black;
        if (pixel.IsGray()) return ColorType.Gray;

        return ColorType.Undefined;
    }

    // rgb(245, 246, 249)
    public static bool IsWhite(this Rgba32 pixel) => pixel.R > 240 && pixel.G > 240 && pixel.B > 240;

    // rgb(223, 227, 238)
    public static bool IsGray(this Rgba32 pixel) =>
        (pixel.R is > 220 and < 240) && (pixel.G is > 220 and < 240) && (pixel.B is > 220 and < 240);

    // rgb(135, 46, 108)
    // rgb(127, 35, 99)
    // rgb(128, 4, 88)
    // rgb(130, 4, 129)
    public static bool IsPurple(this Rgba32 pixel) =>
        (pixel.R is > 125 and < 140) && pixel.G is < 50 && pixel.B is > 85 and < 130;

    // rgb(60, 136, 117)
    public static bool IsGreen(this Rgba32 pixel) =>
        (pixel.R is > 55 and < 65) && (pixel.G is > 130 and < 140) && (pixel.B is > 110 and < 120);

    // rgb(22, 24, 3)
    // rgb(24, 25, 12)
    public static bool IsBlack(this Rgba32 pixel) =>
        (pixel.R is > 20 and < 30) && (pixel.G is > 20 and < 30) && (pixel.B < 20);
}
