namespace PerfusionAnalyzer.Core.Math;

public static class PerfusionCalculator
{
    public static double CalculateAUC_Rect(double[] time, double[] curve)
    {
        double auc = 0;
        for (int i = 1; i < time.Length; i++)
        {
            double dt = time[i] - time[i - 1];
            auc += curve[i - 1] * dt;
        }
        return auc;
    }

    public static double CalculateAUC_Trapezoid(double[] time, double[] curve)
    {
        double auc = 0;
        for (int i = 1; i < time.Length; i++)
        {
            double dt = time[i] - time[i - 1];
            double avgHeight = (curve[i] + curve[i - 1]) / 2.0;
            auc += avgHeight * dt;
        }
        return auc;
    }

    public static double CalculateAUC_Combined(double[] time, double[] curve, double alpha = 0.5)
    {
        double rect = CalculateAUC_Rect(time, curve);
        double trap = CalculateAUC_Trapezoid(time, curve);
        return alpha * rect + (1 - alpha) * trap;
    }

    public static double CalculateAUC_Parabolic(double[] time, double[] curve)
    {
        int n = time.Length;

        if (n != curve.Length || n < 2)
            return 0;

        double auc = 0;

        int simpsonEnd = n % 2 == 0 ? n - 1 : n;

        for (int i = 0; i < simpsonEnd - 2; i += 2)
        {
            double h = time[i + 2] - time[i];
            double c0 = curve[i];
            double c1 = curve[i + 1];
            double c2 = curve[i + 2];
            auc += h / 6.0 * (c0 + 4 * c1 + c2);
        }

        if (simpsonEnd < n)
        {
            double h = time[n - 1] - time[n - 2];
            double c0 = curve[n - 2];
            double c1 = curve[n - 1];
            auc += h / 2.0 * (c0 + c1);
        }

        return auc;
    }

    public static double CalculateMTT(double[] time, double[] curve)
    {
        double auc = CalculateAUC_Combined(time, curve);
        if (auc == 0) return 0;

        double weightedSum = 0;
        for (int i = 1; i < time.Length; i++)
        {
            double dt = time[i] - time[i - 1];
            double avgTime = (time[i] + time[i - 1]) / 2;
            double avgConc = (curve[i] + curve[i - 1]) / 2;
            weightedSum += avgTime * avgConc * dt;
        }

        return weightedSum / auc;
    }

    public static double CalculateTTP(double[] time, double[] curve)
    {
        double maxConcentration = curve.Max();
        int maxIndex = Array.IndexOf(curve, maxConcentration);
        return time[maxIndex];
    }
}