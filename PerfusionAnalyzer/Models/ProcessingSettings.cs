namespace PerfusionAnalyzer.Models;

public class ProcessingSettings
{
    public CurveType CurveType = CurveType.Concentration;
    public FilterType FilterType { get; set; } = FilterType.Gaussian;
    public double LeakageCoefficient { get; set; } = 0.0;
    public int ContrastArrivalPercent { get; set; } = 10;
    public double Gamma { get; set; } = 1.0;
    public ushort BackgroundThreshold { get; set; } = 75;
}