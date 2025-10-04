namespace PerfusionAnalyzer.Core.Filters;

public static class SignalFilter
{
    /// <summary>
    /// Рухоме середнє (Moving Average Filter)
    /// </summary>
    public static double[] ApplyMovingAverage(double[] data, int windowSize = 3)
    {
        int n = data.Length;
        double[] result = new double[n];
        int half = windowSize / 2;

        for (int i = 0; i < n; i++)
        {
            double sum = 0;
            int count = 0;
            for (int j = -half; j <= half; j++)
            {
                int idx = i + j;
                if (idx >= 0 && idx < n)
                {
                    sum += data[idx];
                    count++;
                }
            }
            result[i] = sum / count;
        }

        return result;
    }

    /// <summary>
    /// Гаусів фільтр (Gaussian Filter)
    /// </summary>
    public static double[] ApplyGaussianFilter(double[] data, int radius = 2, double sigma = 1.0)
    {
        int size = radius * 2 + 1;
        double[] kernel = new double[size];
        double sum = 0;

        for (int i = 0; i < size; i++)
        {
            int x = i - radius;
            kernel[i] = System.Math.Exp(-(x * x) / (2 * sigma * sigma));
            sum += kernel[i];
        }

        for (int i = 0; i < size; i++)
            kernel[i] /= sum;

        double[] result = new double[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            double value = 0;
            for (int j = -radius; j <= radius; j++)
            {
                int index = i + j;
                if (index < 0) index = 0;
                if (index >= data.Length) index = data.Length - 1;

                value += data[index] * kernel[j + radius];
            }
            result[i] = value;
        }

        return result;
    }

    /// <summary>
    /// Медіанний фільтр (Median Filter)
    /// </summary>
    public static double[] ApplyMedianFilter(double[] data, int windowSize = 3)
    {
        int n = data.Length;
        double[] result = new double[n];
        int half = windowSize / 2;

        for (int i = 0; i < n; i++)
        {
            List<double> window = new();

            for (int j = -half; j <= half; j++)
            {
                int idx = i + j;
                if (idx < 0) idx = 0;
                if (idx >= n) idx = n - 1;
                window.Add(data[idx]);
            }

            window.Sort();
            result[i] = window[window.Count / 2];
        }

        return result;
    }

    public static void ApplyGammaCorrection(float[,] map, bool[,] mask, double gamma = 1.0)
    {
        int h = map.GetLength(0), w = map.GetLength(1);
        float min = float.MaxValue, max = float.MinValue;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mask[y, x])
                {
                    float val = map[y, x];
                    if (val < min) min = val;
                    if (val > max) max = val;
                }

        float range = max - min;
        if (range < 1e-5f) return;

        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                if (!mask[y, x])
                {
                    map[y, x] = 0;
                    continue;
                }

                float normalized = (map[y, x] - min) / range;
                map[y, x] = (float)System.Math.Pow(normalized, gamma);
            }
        });
    }
}