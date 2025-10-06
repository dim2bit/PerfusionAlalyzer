namespace PerfusionAnalyzer.Models;

public class PerfusionMetrics
{
    public float AUC { get; set; }
    public float MTT { get; set; }
    public float TTP { get; set; }
    public double[] Time { get; set; }
    public double[] Curve { get; set; }
    public double[] SlicedTime { get; set; }
    public double[] SlicedCurve { get; set; }
}