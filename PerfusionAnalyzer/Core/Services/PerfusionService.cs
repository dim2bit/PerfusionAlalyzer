using Dicom.Imaging;
using PerfusionAnalyzer.Core.Dicom;
using PerfusionAnalyzer.Core.Filters;
using PerfusionAnalyzer.Core.Math;
using PerfusionAnalyzer.Models;

namespace PerfusionAnalyzer.Core.Services
{
    public class PerfusionService
    {
        private const int _baselineCount = 5;
        private const int _interpolationStepsPerInterval = 10;

        private readonly int _height;
        private readonly int _width;
        private readonly List<DicomImage> _frames;
        private readonly ushort[][] _ushortFrames;
        private readonly double[] _timePoints;

        public PerfusionService(List<DicomImage> frames)
        {
            if (frames == null || frames.Count == 0)
                throw new InvalidOperationException("Немає завантажених зображень для обробки.");

            var timePoints = DicomUtils.GetTimePoints(frames);

            if (timePoints.Any(t => t < 0))
                throw new InvalidOperationException("Одне або кілька зображень не містять коректного часу (TriggerTime).");

            if (timePoints.Distinct().Count() <= 1)
                throw new InvalidOperationException("Недостатньо різних часових точок для аналізу. Всі кадри мають однаковий час.");

            _height = frames[0].Height;
            _width = frames[0].Width;

            _frames = frames;
            _timePoints = timePoints;
            _ushortFrames = DicomUtils.FramesToUshort(frames, _width, _height);
        }

        public async Task<PerfusionMetrics> CalculateMetricsAsync()
        {
            return await Task.Run(() =>
            {
                var results = new PerfusionMetrics();

                double[] intensityCurve = new double[_frames.Count];
                for (int f = 0; f < _frames.Count; f++)
                {
                    intensityCurve[f] = _ushortFrames[f].Average(p => (double)p);
                }

                int baselineCount = System.Math.Min(_baselineCount, intensityCurve.Length);
                double S0 = intensityCurve.Take(baselineCount).Average();

                double TE_seconds = DicomUtils.GetEchoTime(_frames[0]);

                double[] concentrationCurve = new double[_frames.Count];
                for (int f = 0; f < _frames.Count; f++)
                {
                    concentrationCurve[f] = -1.0 / TE_seconds * System.Math.Log(intensityCurve[f] / S0);
                }

                double[] filteredCurve = SignalFilter.ApplyGaussianFilter(concentrationCurve);

                var spline = SplineInterpolator.GetSpline(_timePoints, filteredCurve);

                var denseTimePoints = SplineInterpolator.GetTimePoints(_timePoints, _interpolationStepsPerInterval);
                var interpolatedCurve = SplineInterpolator.InterpolateCurve(spline, denseTimePoints);

                results.AUCResult = PerfusionCalculator.CalculateAUC(denseTimePoints, interpolatedCurve).ToString("F2");
                results.MTTResult = PerfusionCalculator.CalculateMTT(denseTimePoints, interpolatedCurve).ToString("F2");
                results.TTPResult = PerfusionCalculator.CalculateTTP(denseTimePoints, interpolatedCurve).ToString("F2");

                results.TimePoints = denseTimePoints;
                results.ConcentrationPoints = interpolatedCurve;

                return results;
            });
        }

        public async Task<PerfusionMaps> CalculateMapsAsync()
        {
            return await Task.Run(() =>
            {
                var results = new PerfusionMaps();

                int baselineCount = System.Math.Min(_baselineCount, _frames.Count);

                double TE_seconds = DicomUtils.GetEchoTime(_frames[0]);

                var denseTimePoints = SplineInterpolator.GetTimePoints(_timePoints, _interpolationStepsPerInterval);

                results.AUCMap = new float[_height, _width];
                results.MTTMap = new float[_height, _width];
                results.TTPMap = new float[_height, _width];

                Parallel.For(0, _height, y =>
                {
                    for (int x = 0; x < _width; x++)
                    {
                        double[] pixelCurve = new double[_frames.Count];

                        for (int f = 0; f < _frames.Count; f++)
                        {
                            ushort pixelValue = _ushortFrames[f][y * _width + x];
                            pixelCurve[f] = pixelValue;
                        }

                        double pixelS0 = pixelCurve.Take(baselineCount).Average();

                        double[] mapConcentrationCurve = new double[_frames.Count];
                        for (int f = 0; f < _frames.Count; f++)
                        {
                            if (pixelCurve[f] <= 0 || pixelS0 <= 0)
                            {
                                mapConcentrationCurve[f] = 0;
                            }
                            else
                            {
                                mapConcentrationCurve[f] = -1.0 / TE_seconds * System.Math.Log(pixelCurve[f] / pixelS0);
                            }
                        }

                        double[] filteredMapCurve = SignalFilter.ApplyGaussianFilter(mapConcentrationCurve);

                        var mapSpline = SplineInterpolator.GetSpline(_timePoints, filteredMapCurve);

                        double[] interpolatedMapCurve = SplineInterpolator.InterpolateCurve(mapSpline, denseTimePoints);

                        results.AUCMap[y, x] = (float)PerfusionCalculator.CalculateAUC(denseTimePoints, interpolatedMapCurve);
                        results.MTTMap[y, x] = System.Math.Clamp((float)PerfusionCalculator.CalculateMTT(denseTimePoints, interpolatedMapCurve), 0, 50);
                        results.TTPMap[y, x] = (float)PerfusionCalculator.CalculateTTP(denseTimePoints, interpolatedMapCurve);
                    }
                });

                return results;
            });
        }

        public async Task<PerfusionMaps> PostProcessMapsAsync(
            FilterType filterType, PerfusionMaps originalMaps, ushort threshold, int kernelSize, double gamma)
        {
            bool[,] mask = DicomUtils.CreateMask(_ushortFrames[0], _width, _height, threshold);

            Task<float[,]> ProcessMapAsync(float[,] map) => Task.Run(() =>
            {
                var filtered = ApplyFilter(map, mask, filterType, kernelSize);
                SignalFilter.ApplyGammaCorrection(filtered, mask, gamma);
                return filtered;
            });

            var aucTask = ProcessMapAsync(originalMaps.AUCMap);
            var mttTask = ProcessMapAsync(originalMaps.MTTMap);
            var ttpTask = ProcessMapAsync(originalMaps.TTPMap);

            await Task.WhenAll(aucTask, mttTask, ttpTask);

            return new PerfusionMaps
            {
                AUCMap = await aucTask,
                MTTMap = await mttTask,
                TTPMap = await ttpTask
            };
        }

        private float[,] ApplyFilter(float[,] map, bool[,] mask, FilterType filterType, int kernelSize)
        {
            return filterType switch
            {
                FilterType.Median => SignalFilter.ApplyMaskedMedianFilter(map, mask, kernelSize),
                FilterType.Gaussian => throw new NotImplementedException(),
                FilterType.Bilateral => SignalFilter.ApplyMaskedBilateralFilter(map, mask, kernelSize),
                FilterType.None => map,
                _ => throw new ArgumentOutOfRangeException(nameof(filterType), filterType, null)
            };
        }
    }
}