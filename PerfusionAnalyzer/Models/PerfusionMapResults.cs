namespace PerfusionAnalyzer.Models;

public class PerfusionMapResults
{
    public float[,] AucMap { get; set; }
    public float[,] MttMap { get; set; }
    public float[,] TtpMap { get; set; }
}