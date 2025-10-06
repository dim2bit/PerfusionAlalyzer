using Dicom.Imaging;
using PerfusionAnalyzer.Core.Dicom;
using PerfusionAnalyzer.Core.Filters;
using PerfusionAnalyzer.Core.Math;
using PerfusionAnalyzer.Core.Utils;
using PerfusionAnalyzer.Models;

namespace PerfusionAnalyzer.Core.Services;

public class PerfusionService
{
    private const int _interpolationStepsPerInterval = 10;
    private const double _contrastRecirculationPercent = 50;

    private readonly double _TE_seconds;
    private readonly int _baselineCount = 3;

    private readonly int _height;
    private readonly int _width;
    private readonly List<DicomImage> _frames;
    private readonly ushort[][] _ushortFrames;
    private readonly double[] _time;

    public PerfusionService(List<DicomImage> frames)
    {
        if (frames == null || frames.Count == 0)
            throw new InvalidOperationException("Немає завантажених зображень для обробки.");

        var time = DicomUtils.GetTimePoints(frames);

        if (time.Any(t => t < 0))
            throw new InvalidOperationException("Одне або кілька зображень не містять часу зйомки (TriggerTime).");

        if (time.Distinct().Count() <= 1)
            throw new InvalidOperationException("Недостатньо різних часових точок для аналізу. Всі кадри мають однаковий час.");

        _height = frames[0].Height;
        _width = frames[0].Width;

        _frames = frames;
        _time = time;
        _ushortFrames = DicomUtils.FramesToUshort(frames, _width, _height);
        _TE_seconds = DicomUtils.GetEchoTime(_frames[0]);
        _baselineCount = System.Math.Min(_baselineCount, _frames.Count);
    }

    public async Task<PerfusionMetrics> CalculateMetricsAsync(ProcessingSettings settings)
    {
        return await Task.Run(() =>
        {
            double[] intensityCurve = _ushortFrames.Select(
                frame => frame.Average(p => (double)p)).ToArray();

            return CalculateMetricsFromCurve(intensityCurve, settings);
        });
    }

    public async Task<PerfusionMaps> CalculateMapsAsync(ProcessingSettings settings)
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

                    var metrics = CalculateMetricsFromCurve(intensityCurve, settings);

                    maps.AUCMap[y, x] = metrics.AUC;
                    maps.MTTMap[y, x] = metrics.MTT;
                    maps.TTPMap[y, x] = metrics.TTP;
                }
            });

            return maps;
        });
    }

    public async Task<PerfusionMaps> PostProcessMapsAsync(PerfusionMaps originalMaps, ProcessingSettings settings)
    {
        bool[,] mask = DicomUtils.CreateMask(_ushortFrames[0], _width, _height, settings.BackgroundThreshold);

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

    private PerfusionMetrics CalculateMetricsFromCurve(double[] intensityCurve, ProcessingSettings settings)
    {
        var metrics = new PerfusionMetrics();
        double[] curve = new double[_frames.Count];

        if (settings.CurveType == CurveType.Concentration)
        {
            double S0 = intensityCurve.Take(_baselineCount).Average();
            for (int f = 0; f < _frames.Count; f++)
            {
                if (intensityCurve[f] <= 0 || S0 <= 0)
                    curve[f] = 0;
                else
                    curve[f] = -1.0 / _TE_seconds * System.Math.Log(intensityCurve[f] / S0);
            }
        }
        else
        {
            curve = (double[])intensityCurve.Clone();
        }

        double[] filteredCurve = ApplyFilter(curve, settings.FilterType);

        filteredCurve = CurveUtils.ApplyLeakageCorrection(_time, filteredCurve, settings.LeakageCoefficient);

        var (slicedTime, slicedCurve) = CurveUtils.ExtractContrastCurve(
            _time, filteredCurve, settings.ContrastArrivalPercent, _contrastRecirculationPercent);

        //double[] fittedCurve = CurveUtils.FitGammaCurve(timeSlice, curveSlice);

        metrics.AUC = (float)PerfusionCalculator.CalculateAUC_Combined(slicedTime, slicedCurve);
        metrics.MTT = System.Math.Clamp((float)PerfusionCalculator.CalculateMTT(slicedTime, slicedCurve), 0, 50);
        metrics.TTP = (float)PerfusionCalculator.CalculateTTP(slicedTime, slicedCurve);

        metrics.Time = _time;
        metrics.Curve = filteredCurve;

        metrics.SlicedTime = slicedTime;
        metrics.SlicedCurve = slicedCurve;

        return metrics;
    }

    private double[] ApplyFilter(double[] curve, FilterType filterType)
    {
        return filterType switch
        {
            FilterType.Gaussian => SignalFilter.ApplyGaussianFilter(curve),
            FilterType.MovingAverage => SignalFilter.ApplyMovingAverage(curve),
            FilterType.None => (double[])curve.Clone(),
            _ => throw new ArgumentException("Невідомий тип фільтрації")
        };
    }

    private async Task<float[,]> PostProcessMapAsync(float[,] map, bool[,] mask, ProcessingSettings settings)
    {
        return await Task.Run(() =>
        {
            var mapCopy = (float[,])map.Clone();
            SignalFilter.ApplyGammaCorrection(mapCopy, mask, settings.Gamma);
            return mapCopy;
        });
    }
}