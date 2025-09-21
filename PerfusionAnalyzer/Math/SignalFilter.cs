namespace PerfusionAnalyzer.Math;

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
}