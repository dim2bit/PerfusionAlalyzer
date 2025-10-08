namespace PerfusionAnalyzer.Models;

public class ProcessingSettings
{
    public CurveType CurveType = CurveType.Concentration;
    public FilterType FilterType { get; set; } = FilterType.Gaussian;
    public bool[,]? Mask { get; set; }
    public double LeakageCoefficient { get; set; } = 0.015;
    public int ContrastArrivalPercent { get; set; } = 30;
    public double Gamma { get; set; } = 1.0;
}