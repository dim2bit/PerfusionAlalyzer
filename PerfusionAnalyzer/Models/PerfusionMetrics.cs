namespace PerfusionAnalyzer.Models;

public class PerfusionMetrics
{
    public float AUC { get; set; }
    public float MTT { get; set; }
    public float TTP { get; set; }
    public double[] TimePoints { get; set; }
    public double[] ConcentrationPoints { get; set; }
    public double[] SlicedTimePoints { get; set; }
    public double[] SlicedConcentrationPoints { get; set; }
}