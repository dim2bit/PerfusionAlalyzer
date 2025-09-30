using Dicom.Imaging;
using PerfusionAnalyzer.Core.Dicom;
using PerfusionAnalyzer.Core.Filters;
using PerfusionAnalyzer.Core.Math;
using PerfusionAnalyzer.Models;

namespace PerfusionAnalyzer.Core.Services;

public class PerfusionService
{
    private const int _interpolationStepsPerInterval = 10;

    private readonly double _TE_seconds;
    private readonly int _baselineCount = 5;

    private readonly int _height;
    private readonly int _width;
    private readonly List<DicomImage> _frames;
    private readonly ushort[][] _ushortFrames;
    private readonly double[] _timePoints;
    private readonly double[] _denseTimePoints;

    public PerfusionService(List<DicomImage> frames)
    {
        if (frames == null || frames.Count == 0)
            throw new InvalidOperationException("Немає завантажених зображень для обробки.");

        var timePoints = DicomUtils.GetTimePoints(frames);

        if (timePoints.Any(t => t < 0))
            throw new InvalidOperationException("Одне або кілька зображень не містять часу зйомки (TriggerTime).");

        if (timePoints.Distinct().Count() <= 1)
            throw new InvalidOperationException("Недостатньо різних часових точок для аналізу. Всі кадри мають однаковий час.");

        _height = frames[0].Height;
        _width = frames[0].Width;

        _frames = frames;
        _timePoints = timePoints;
        _denseTimePoints = SplineInterpolator.GetTimePoints(_timePoints, _interpolationStepsPerInterval);
        _ushortFrames = DicomUtils.FramesToUshort(frames, _width, _height);
        _TE_seconds = DicomUtils.GetEchoTime(_frames[0]);
        _baselineCount = System.Math.Min(_baselineCount, _frames.Count);
    }

    public async Task<PerfusionMetrics> CalculateMetricsAsync()
    {
        return await Task.Run(() =>
        {
            double[] intensityCurve = _ushortFrames.Select(
                frame => frame.Average(p => (double)p)).ToArray();

            return CalculateMetricsFromCurve(intensityCurve);
        });
    }

    public async Task<PerfusionMaps> CalculateMapsAsync()
    {
        return await Task.Run(() =>
        {
            var maps = new PerfusionMaps();

            maps.AUCMap = new float[_height, _width];
            maps.MTTMap = new float[_height, _width];
            maps.TTPMap = new float[_height, _width];

            Parallel.For(0, _height, y =>
            {
                for (int x = 0; x < _width; x++)
                {
                    double[] intensityCurve = _ushortFrames.Select(
                        frame => (double)frame[y * _width + x]).ToArray();

                    var metrics = CalculateMetricsFromCurve(intensityCurve);

                    maps.AUCMap[y, x] = metrics.AUC;
                    maps.MTTMap[y, x] = metrics.MTT;
                    maps.TTPMap[y, x] = metrics.TTP;
                }
            });

            return maps;
        });
    }

    public async Task<PerfusionMaps> PostProcessMapsAsync(PerfusionMaps originalMaps, PostProcessingSettings settings)
    {
        bool[,] mask = DicomUtils.CreateMask(_ushortFrames[0], _width, _height, settings.Threshold);

        var aucTask = PostProcessMapAsync(originalMaps.AUCMap, mask, settings);
        var mttTask = PostProcessMapAsync(originalMaps.MTTMap, mask, settings);
        var ttpTask = PostProcessMapAsync(originalMaps.TTPMap, mask, settings);

        await Task.WhenAll(aucTask, mttTask, ttpTask);

        return new PerfusionMaps
        {
            AUCMap = await aucTask,
            MTTMap = await mttTask,
            TTPMap = await ttpTask
        };
    }

    private PerfusionMetrics CalculateMetricsFromCurve(double[] intensityCurve)
    {
        var metrics = new PerfusionMetrics();

        double S0 = intensityCurve.Take(_baselineCount).Average();

        double[] concentrationCurve = new double[_frames.Count];
        for (int f = 0; f < _frames.Count; f++)
        {
            if (intensityCurve[f] <= 0 || S0 <= 0)
                concentrationCurve[f] = 0;
            else
                concentrationCurve[f] = -1.0 / _TE_seconds * System.Math.Log(intensityCurve[f] / S0);
        }

        double[] filteredCurve = SignalFilter.ApplyGaussianFilter(concentrationCurve);

        var spline = SplineInterpolator.GetSpline(_timePoints, filteredCurve);
        double[] interpolatedCurve = SplineInterpolator.InterpolateCurve(spline, _denseTimePoints);

        metrics.AUC = (float)PerfusionCalculator.CalculateAUC(_denseTimePoints, interpolatedCurve);
        metrics.MTT = System.Math.Clamp((float)PerfusionCalculator.CalculateMTT(_denseTimePoints, interpolatedCurve), 0, 50);
        metrics.TTP = (float)PerfusionCalculator.CalculateTTP(_denseTimePoints, interpolatedCurve);

        metrics.TimePoints = _denseTimePoints;
        metrics.ConcentrationPoints = interpolatedCurve;

        return metrics;
    }

    private async Task<float[,]> PostProcessMapAsync(float[,] map, bool[,] mask, PostProcessingSettings settings)
    {
        return await Task.Run(() =>
        {
            var filtered = ApplyFilter(map, mask, settings.FilterType, settings.KernelSize);
            SignalFilter.ApplyGammaCorrection(filtered, mask, settings.Gamma);
            return filtered;
        });
    }

    private float[,] ApplyFilter(float[,] map, bool[,] mask, FilterType filterType, int kernelSize)
    {
        return filterType switch
        {
            FilterType.Median => SignalFilter.ApplyMaskedMedianFilter(map, mask, kernelSize),
            FilterType.Gaussian => SignalFilter.ApplyMaskedGaussianFilter(map, mask, kernelSize),
            FilterType.Bilateral => SignalFilter.ApplyMaskedBilateralFilter(map, mask, kernelSize),
            FilterType.None => (float[,])map.Clone(),
            _ => throw new ArgumentOutOfRangeException(nameof(filterType), filterType, null)
        };
    }
}