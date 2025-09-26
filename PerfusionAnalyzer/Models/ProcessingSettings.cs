namespace PerfusionAnalyzer.Models;

public class ProcessingSettings
{
    public double Gamma { get; set; } = 0.25;
    public int KernelSize { get; set; } = 3;
    public ushort Threshold { get; set; } = 75;
    public bool IsPostProcessingEnabled { get; set; } = true;
}