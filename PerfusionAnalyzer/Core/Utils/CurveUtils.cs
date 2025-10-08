namespace PerfusionAnalyzer.Core.Utils;

public class CurveUtils
{
    public static (double[] Time, double[] Curve) ExtractContrastCurve(
        double[] time,
        double[] curve,
        double contrastArrivalPercent,
        double contrastRecirculationPercent)
    {
        double peak = curve.Max();
        int peakIndex = Array.IndexOf(curve, peak);

        double thresholdStart = contrastArrivalPercent / 100.0 * peak;
        double thresholdReCirc = contrastRecirculationPercent / 100.0 * peak;

        double? tStart = FindThresholdTime(time, curve, thresholdStart, rising: true);
        double? tEnd = FindThresholdTime(time, curve, thresholdReCirc, rising: false, startIndex: peakIndex);

        if (tStart == null)
        {
            tStart = time.First();
        }
        if (tEnd == null)
        {
            tEnd = time.Last();
        }
        if (tStart >= tEnd)
        {
            tStart = time.First();
            tEnd = time.Last();
        }

        return CutCurveBetweenThresholds(time, curve, tStart.Value, tEnd.Value);
    }

    public static double[] ApplyLeakageCorrection(double[] time, double[] curve, double leakageCoefficient)
    {
        var corrected = new double[curve.Length];
        var integral = new double[curve.Length];

        integral[0] = 0;
        for (int i = 1; i < curve.Length; i++)
        {
            double dt = time[i] - time[i - 1];
            integral[i] = integral[i - 1] + 0.5 * (curve[i] + curve[i - 1]) * dt;
        }

        for (int i = 0; i < curve.Length; i++)
        {
            corrected[i] = curve[i] - leakageCoefficient * integral[i];
        }

        return corrected;
    }

    private static double? FindThresholdTime(double[] time, double[] curve, double threshold, bool rising, int startIndex = 0)
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

    private static (double[] Time, double[] Curve) CutCurveBetweenThresholds(double[] time, double[] curve, double tStart, double tEnd)
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

    private static double Interpolate(double x0, double y0, double x1, double y1, double x)
    {
        if (x1 == x0) return y0;
        return y0 + (x - x0) / (x1 - x0) * (y1 - y0);
    }
}