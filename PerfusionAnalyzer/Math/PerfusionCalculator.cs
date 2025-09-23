namespace PerfusionAnalyzer.Math;

public static class PerfusionCalculator
{
    public static double CalculateAUC(double[] timePoints, double[] concentrationPoints)
    {
        int n = timePoints.Length;

        if (n != concentrationPoints.Length || n < 2)
            return 0;

        double auc = 0;

        int simpsonEnd = (n % 2 == 0) ? n - 1 : n;

        for (int i = 0; i < simpsonEnd - 2; i += 2)
        {
            double h = timePoints[i + 2] - timePoints[i];
            double c0 = concentrationPoints[i];
            double c1 = concentrationPoints[i + 1];
            double c2 = concentrationPoints[i + 2];
            auc += (h / 6.0) * (c0 + 4 * c1 + c2);
        }

        if (simpsonEnd < n)
        {
            double h = timePoints[n - 1] - timePoints[n - 2];
            double c0 = concentrationPoints[n - 2];
            double c1 = concentrationPoints[n - 1];
            auc += (h / 2.0) * (c0 + c1);
        }

        return auc;
    }

    public static double CalculateMTT(double[] timePoints, double[] concentrationPoints)
    {
        double auc = CalculateAUC(timePoints, concentrationPoints);
        if (auc == 0) return 0;

        double weightedSum = 0;
        for (int i = 1; i < timePoints.Length; i++)
        {
            double dt = timePoints[i] - timePoints[i - 1];
            double avgTime = (timePoints[i] + timePoints[i - 1]) / 2;
            double avgConc = (concentrationPoints[i] + concentrationPoints[i - 1]) / 2;
            weightedSum += avgTime * avgConc * dt;
        }

        return weightedSum / auc;
    }

    public static double CalculateTTP(double[] timePoints, double[] concentrationPoints)
    {
        double maxConcentration = concentrationPoints.Max();
        int maxIndex = Array.IndexOf(concentrationPoints, maxConcentration);
        return timePoints[maxIndex];
    }
}