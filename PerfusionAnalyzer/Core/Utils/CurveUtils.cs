namespace PerfusionAnalyzer.Core.Utils;

public class CurveUtils
{
    public static double? FindThresholdTime(double[] time, double[] curve, double threshold, bool rising, int startIndex = 0)
    {
        for (int i = startIndex; i < curve.Length - 1; i++)
        {
            if (rising && curve[i] < threshold && curve[i + 1] >= threshold ||
                !rising && curve[i] > threshold && curve[i + 1] <= threshold)
            {
                double t0 = time[i];
                double t1 = time[i + 1];
                double c0 = curve[i];
                double c1 = curve[i + 1];

                if (c1 == c0) return t0;

                double alpha = (threshold - c0) / (c1 - c0);
                return t0 + alpha * (t1 - t0);
            }
        }
        return null;
    }

    public static (double[] Time, double[] Curve) CutCurveBetweenThresholds(double[] time, double[] curve, double tStart, double tEnd)
    {
        List<double> tOut = new();
        List<double> cOut = new();

        for (int i = 0; i < time.Length - 1; i++)
        {
            if (time[i + 1] < tStart || time[i] > tEnd)
                continue;

            if (time[i] < tStart && time[i + 1] >= tStart)
            {
                double c = Interpolate(time[i], curve[i], time[i + 1], curve[i + 1], tStart);
                tOut.Add(tStart);
                cOut.Add(c);
            }

            if (time[i] >= tStart && time[i] <= tEnd)
            {
                tOut.Add(time[i]);
                cOut.Add(curve[i]);
            }

            if (time[i] < tEnd && time[i + 1] >= tEnd)
            {
                double c = Interpolate(time[i], curve[i], time[i + 1], curve[i + 1], tEnd);
                tOut.Add(tEnd);
                cOut.Add(c);
            }
        }

        return (tOut.ToArray(), cOut.ToArray());
    }

    public static double Interpolate(double x0, double y0, double x1, double y1, double x)
    {
        if (x1 == x0) return y0;
        return y0 + (x - x0) / (x1 - x0) * (y1 - y0);
    }
}