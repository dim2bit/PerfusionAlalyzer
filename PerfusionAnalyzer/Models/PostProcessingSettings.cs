namespace PerfusionAnalyzer.Models;

public class PostProcessingSettings
{
    public bool IsEnabled { get; set; } = true;
    public FilterType FilterType { get; set; } = FilterType.Median;
    public double Gamma { get; set; } = 0.25;
    public int KernelSize { get; set; } = 3;
    public ushort Threshold { get; set; } = 75;
}