namespace PerfusionAnalyzer.Core.Utils;

public static class PerfusionColorMapsUtils
{
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

    public static Color GetAUCColor(float value, float min, float max)
    {
        double t = Normalize(value, min, max);

        if (t < 0.33)
            return LerpColor(Color.Blue, Color.Green, t / 0.33);
        else if (t < 0.66)
            return LerpColor(Color.Green, Color.Yellow, (t - 0.33) / 0.33);
        else
            return LerpColor(Color.Yellow, Color.Red, (t - 0.66) / 0.34);
    }

    public static Color GetMTTColor(float value, float min, float max)
    {
        double t = Normalize(value, min, max);

        if (t < 0.33)
            return LerpColor(Color.Blue, Color.Yellow, t / 0.33);
        else if (t < 0.66)
            return LerpColor(Color.Cyan, Color.Orange, (t - 0.33) / 0.33);
        else
            return LerpColor(Color.Orange, Color.Red, (t - 0.66) / 0.34);
    }

    public static Color GetTTPColor(float value, float min, float max)
    {
        double t = Normalize(value, min, max);

        if (t < 0.33)
            return LerpColor(Color.Blue, Color.Cyan, t / 0.33);
        else if (t < 0.66)
            return LerpColor(Color.Cyan, Color.Yellow, (t - 0.33) / 0.33);
        else
            return LerpColor(Color.Yellow, Color.Red, (t - 0.66) / 0.34);
    }
}