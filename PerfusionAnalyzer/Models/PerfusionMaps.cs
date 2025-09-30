namespace PerfusionAnalyzer.Models;

public class PerfusionMaps
{
    public float[,] AUCMap { get; set; }
    public float[,] MTTMap { get; set; }
    public float[,] TTPMap { get; set; }
}