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
        //if (pixel.IsDarkGray()) return ColorType.DarkGray;

        return ColorType.Undefined;
    }

    // rgb(245, 246, 249)
    public static bool IsWhite(this Rgba32 pixel) => pixel.R > 240 && pixel.G > 240 && pixel.B > 240;

    // rgb(223, 227, 238)
    public static bool IsGray(this Rgba32 pixel)
    {
        var whiteThreshold = 240;
        if (pixel.R > whiteThreshold || pixel.G > whiteThreshold || pixel.B > whiteThreshold) return false;

        var rg = pixel.R - pixel.G;
        var rb = pixel.R - pixel.B;
        var gb = pixel.G - pixel.B;

        return Math.Abs(rg) < 20 && Math.Abs(rb) < 20 && Math.Abs(gb) < 20;
    }

    // rgb(135, 46, 108)
    // rgb(127, 35, 99)
    // rgb(128, 4, 88)
    // rgb(130, 4, 129)
    public static bool IsPurple(this Rgba32 pixel) =>
        (pixel.R is > 125 and < 140) && pixel.G is < 50 && pixel.B is > 85 and < 130;

    // rgb(60, 136, 117)

    // rgb(58, 136, 148)
    // rgb(53, 138, 145)
    // rgb(66, 130, 137)
    // rgb(85, 127, 135)
    // rgb(85, 141, 140)
    // rgb(125, 171, 175)

    public static bool IsGreen(this Rgba32 pixel) =>
        (pixel.R is > 55 and < 88) && (pixel.G is > 120 and < 145) && (pixel.B is > 110 and < 150);

    // rgb(22, 24, 3)
    // rgb(24, 25, 12)
    public static bool IsBlack(this Rgba32 pixel) => (pixel.R is < 30) && (pixel.G < 30) && (pixel.B < 30);
}
