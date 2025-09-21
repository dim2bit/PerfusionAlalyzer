namespace PerfusionAnalyzer.Math;

public static class PerfusionCalculator
{
    public static double CalculateAUC(double[] timePoints, double[] concentrationCurve)
    {
        double auc = 0;
        for (int i = 1; i < timePoints.Length; i++)
        {
            double dt = timePoints[i] - timePoints[i - 1];
            double avgHeight = (concentrationCurve[i] + concentrationCurve[i - 1]) / 2;
            auc += dt * avgHeight;
        }
        return auc;
    }

    public static double CalculateMTT(double[] timePoints, double[] concentrationCurve)
    {
        double auc = CalculateAUC(timePoints, concentrationCurve);
        double cmax = concentrationCurve.Max();
        if (cmax == 0) return 0;
        return auc / cmax;
    }

    public static double CalculateTTP(double[] timePoints, double[] concentrationCurve)
    {
        int maxIndex = 0;
        double maxVal = concentrationCurve[0];
        for (int i = 1; i < concentrationCurve.Length; i++)
        {
            if (concentrationCurve[i] > maxVal)
            {
                maxVal = concentrationCurve[i];
                maxIndex = i;
            }
        }
        return timePoints[maxIndex];
    }
}