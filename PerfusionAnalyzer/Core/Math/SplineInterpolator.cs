using MathNet.Numerics.Interpolation;

namespace PerfusionAnalyzer.Core.Math;

public class SplineInterpolator
{
    public static CubicSpline GetSpline(double[] timePoints, double[] concentrationCurve)
    {
        return CubicSpline.InterpolateNaturalSorted(timePoints, concentrationCurve);
    }

    public static double[] GetTimePoints(double[] timePoints, int stepsPerInterval)
    {
        double min = timePoints.First();
        double max = timePoints.Last();

        double originalStep = (max - min) / (timePoints.Length - 1);
        double interpStep = originalStep / stepsPerInterval;

        int count = (int)System.Math.Round((max - min) / interpStep) + 1;

        double[] dense = new double[count];
        for (int i = 0; i < count; i++)
        {
            dense[i] = min + i * interpStep;
        }

        return dense;
    }

    public static double[] InterpolateCurve(CubicSpline spline, double[] newTimePoints)
    {
        double[] interpolated = new double[newTimePoints.Length];
        for (int i = 0; i < newTimePoints.Length; i++)
        {
            var interpolatedPoint = spline.Interpolate(newTimePoints[i]);
            interpolated[i] = double.IsNaN(interpolatedPoint) ? 0.0 : interpolatedPoint;
        }
        return interpolated;
    }
}