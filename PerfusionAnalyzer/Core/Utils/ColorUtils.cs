namespace PerfusionAnalyzer.Core.Utils;

public static class ColorUtils
{
    private static readonly Color[] ViridisLUT = new Color[]
    {
        ColorTranslator.FromHtml("#440154"),
        ColorTranslator.FromHtml("#482777"),
        ColorTranslator.FromHtml("#3b528b"),
        ColorTranslator.FromHtml("#2c728e"),
        ColorTranslator.FromHtml("#21908d"),
        ColorTranslator.FromHtml("#27ad81"),
        ColorTranslator.FromHtml("#5ec962"),
        ColorTranslator.FromHtml("#aadc32"),
        ColorTranslator.FromHtml("#fde725")
    };

    public static Color GetViridisColor(float value, float min, float max)
    {
        double t = Normalize(value, min, max);
        int index1 = (int)(t * (ViridisLUT.Length - 1));
        int index2 = System.Math.Min(index1 + 1, ViridisLUT.Length - 1);
        double frac = (t * (ViridisLUT.Length - 1)) - index1;

        return LerpColor(ViridisLUT[index1], ViridisLUT[index2], frac);
    }

    private static double Normalize(float value, float min, float max)
    {
        if (max - min == 0) return 0;
        return (value - min) / (max - min);
    }

    private static Color LerpColor(Color c1, Color c2, double t)
    {
        int r = (int)(c1.R + (c2.R - c1.R) * t);
        int g = (int)(c1.G + (c2.G - c1.G) * t);
        int b = (int)(c1.B + (c2.B - c1.B) * t);
        return Color.FromArgb(r, g, b);
    }
}