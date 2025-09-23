using MathNet.Numerics.Interpolation;

namespace PerfusionAnalyzer.Math
{
    public class SplineInterpolator
    {
        public static CubicSpline GetSpline(double[] timePoints, double[] concentrationCurve)
        {
            return CubicSpline.InterpolateNaturalSorted(timePoints, concentrationCurve);
        }

        public static double[] InterpolateCurve(CubicSpline spline, double[] newTimePoints)
        {
            double[] interpolated = new double[newTimePoints.Length];
            for (int i = 0; i < newTimePoints.Length; i++)
                interpolated[i] = spline.Interpolate(newTimePoints[i]);
            return interpolated;
        }
    }
}