namespace PerfusionAnalyzer.Models;

public class PerfusionMetrics
{
    public string AUCResult { get; set; }
    public string MTTResult { get; set; }
    public string TTPResult { get; set; }
    public double[] TimePoints { get; set; }
    public double[] ConcentrationPoints { get; set; }
}