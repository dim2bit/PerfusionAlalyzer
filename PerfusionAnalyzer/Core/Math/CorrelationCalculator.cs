namespace PerfusionAnalyzer.Core.Math;

public class CorrelationCalculator
{
    public static double CalculatePearson(float[,] map1, float[,] map2)
    {
        int height = map1.GetLength(0);
        int width = map1.GetLength(1);

        if (height != map2.GetLength(0) || width != map2.GetLength(1))
            throw new ArgumentException("Розміри карт не співпадають");

        List<double> values1 = new();
        List<double> values2 = new();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v1 = map1[y, x];
                float v2 = map2[y, x];

                if (float.IsNaN(v1) || float.IsNaN(v2))
                    continue;

                values1.Add(v1);
                values2.Add(v2);
            }
        }

        if (values1.Count == 0)
            return double.NaN;

        return Pearson(values1, values2);
    }

    private static double Pearson(List<double> x, List<double> y)
    {
        if (x.Count != y.Count || x.Count == 0)
            return double.NaN;

        double avgX = x.Average();
        double avgY = y.Average();

        double numerator = 0;
        double sumSqX = 0;
        double sumSqY = 0;

        for (int i = 0; i < x.Count; i++)
        {
            double dx = x[i] - avgX;
            double dy = y[i] - avgY;

            numerator += dx * dy;
            sumSqX += dx * dx;
            sumSqY += dy * dy;
        }

        double denominator = System.Math.Sqrt(sumSqX * sumSqY);
        if (denominator < 1e-10) return 0;

        return numerator / denominator;
    }
}
