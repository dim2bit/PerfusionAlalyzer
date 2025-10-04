namespace PerfusionAnalyzer.Models;

public class ProcessingSettings
{
    public FilterType FilterType { get; set; } = FilterType.Gaussian;
    public double Gamma { get; set; } = 0.25;
    public ushort BackgroundThreshold { get; set; } = 75;
    public int ContrastArrivalPercent { get; set; } = 10;
}