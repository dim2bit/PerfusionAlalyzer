namespace PerfusionAnalyzer.Core.Filters;

public static class SignalFilter
{
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

    public static float[,] ApplyMaskedMedianFilter(float[,] map, bool[,] mask, int kernelSize = 3)
    {
        int h = map.GetLength(0), w = map.GetLength(1);
        float[,] result = new float[h, w];
        int k = kernelSize / 2;

        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                if (!mask[y, x]) { result[y, x] = 0; continue; }

                List<float> neighbors = new();

                for (int dy = -k; dy <= k; dy++)
                    for (int dx = -k; dx <= k; dx++)
                    {
                        int ny = y + dy, nx = x + dx;
                        if (ny >= 0 && ny < h && nx >= 0 && nx < w && mask[ny, nx])
                            neighbors.Add(map[ny, nx]);
                    }

                if (neighbors.Count > 0)
                {
                    neighbors.Sort();
                    result[y, x] = neighbors[neighbors.Count / 2];
                }
                else result[y, x] = map[y, x];
            }
        });

        return result;
    }

    public static float[,] ApplyMaskedBilateralFilter(float[,] map, bool[,] mask, int kernelSize = 3, double sigmaSpatial = 2.0, double sigmaRange = 25.0)
    {
        int h = map.GetLength(0), w = map.GetLength(1);
        float[,] result = new float[h, w];
        int k = kernelSize / 2;

        double twoSigmaSpatial2 = 2 * sigmaSpatial * sigmaSpatial;
        double twoSigmaRange2 = 2 * sigmaRange * sigmaRange;

        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                if (!mask[y, x]) { result[y, x] = 0; continue; }

                double sum = 0.0;
                double weightSum = 0.0;
                float centerVal = map[y, x];

                for (int dy = -k; dy <= k; dy++)
                {
                    for (int dx = -k; dx <= k; dx++)
                    {
                        int ny = y + dy, nx = x + dx;

                        if (ny >= 0 && ny < h && nx >= 0 && nx < w && mask[ny, nx])
                        {
                            float neighborVal = map[ny, nx];
                            double spatialDist2 = dx * dx + dy * dy;
                            double rangeDiff2 = (neighborVal - centerVal) * (neighborVal - centerVal);

                            double weight = System.Math.Exp(-spatialDist2 / twoSigmaSpatial2) * System.Math.Exp(-rangeDiff2 / twoSigmaRange2);

                            sum += neighborVal * weight;
                            weightSum += weight;
                        }
                    }
                }

                result[y, x] = (float)(sum / weightSum);
            }
        });

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