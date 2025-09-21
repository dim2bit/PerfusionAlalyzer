namespace PerfusionAnalyzer.Models;

public class PerfusionResults
{
    public string AUCResult { get; set; }
    public string MTTResult { get; set; }
    public string TTPResult { get; set; }
    public float[,] AucMap { get; set; }
    public float[,] MttMap { get; set; }
    public float[,] TtpMap { get; set; }
    public string ErrorMessage { get; set; }
    public double[] TimePoints { get; set; }
    public double[] FilteredCurve { get; set; }
}