namespace PerfusionAnalyzer.Models;

public class PerfusionMaps
{
    public float[,] AUCMap { get; set; }
    public float[,] MTTMap { get; set; }
    public float[,] TTPMap { get; set; }
    public float[,] AUCMapPreprocessed { get; set; }
    public float[,] MTTMapPreprocessed { get; set; }
    public float[,] TTPMapPreprocessed { get; set; }
}