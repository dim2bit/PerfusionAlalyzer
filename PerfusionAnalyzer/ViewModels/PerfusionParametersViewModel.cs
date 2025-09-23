using Dicom;
using Dicom.Imaging;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PerfusionAnalyzer.Math;
using PerfusionAnalyzer.Models;
using PerfusionAnalyzer.Services;
using PerfusionAnalyzer.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace PerfusionAnalyzer.ViewModels;

public class PerfusionParametersViewModel : INotifyPropertyChanged
{
    private DescriptorType _selectedDescriptor;
    private string _errorMessage = "";

    private string _aucResult = "";
    private string _mttResult = "";
    private string _ttpResult = "";

    private float[,] _aucMap;
    private float[,] _mttMap;
    private float[,] _ttpMap;

    public event EventHandler? DescriptorChanged;

    public ObservableCollection<DescriptorType> AvailableDescriptors { get; } =
        new ObservableCollection<DescriptorType> { DescriptorType.AUC, DescriptorType.MTT, DescriptorType.TTP };

    private PlotModel _perfusionPlotModel;
    public PlotModel PerfusionPlotModel
    {
        get => _perfusionPlotModel;
        set
        {
            if (_perfusionPlotModel != value)
            {
                _perfusionPlotModel = value;
                OnPropertyChanged(nameof(PerfusionPlotModel));
            }
        }
    }

    public DescriptorType SelectedDescriptor
    {
        get => _selectedDescriptor;
        set
        {
            if (_selectedDescriptor != value)
            {
                _selectedDescriptor = value;
                OnPropertyChanged(nameof(SelectedDescriptor));
                DescriptorChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string AUCResult
    {
        get => _aucResult;
        set
        {
            if (_aucResult != value)
            {
                _aucResult = value;
                OnPropertyChanged(nameof(AUCResult));
            }
        }
    }

    public string MTTResult
    {
        get => _mttResult;
        set
        {
            if (_mttResult != value)
            {
                _mttResult = value;
                OnPropertyChanged(nameof(MTTResult));
            }
        }
    }

    public string TTPResult
    {
        get => _ttpResult;
        set
        {
            if (_ttpResult != value)
            {
                _ttpResult = value;
                OnPropertyChanged(nameof(TTPResult));
            }
        }
    }

    public float[,] AucMap
    {
        get => _aucMap;
        set
        {
            if (_aucMap != value)
            {
                _aucMap = value;
                OnPropertyChanged(nameof(AucMap));
            }
        }
    }

    public float[,] MttMap
    {
        get => _mttMap;
        set
        {
            if (_mttMap != value)
            {
                _mttMap = value;
                OnPropertyChanged(nameof(MttMap));
            }
        }
    }

    public float[,] TtpMap
    {
        get => _ttpMap;
        set
        {
            if (_ttpMap != value)
            {
                _ttpMap = value;
                OnPropertyChanged(nameof(TtpMap));
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }
    }

    public ICommand ExportMapCommand { get; }

    public PerfusionParametersViewModel()
    {
        ExportMapCommand = new RelayCommand(_ => ExportCurrentMapToPng(), _ =>
        {
            if (SelectedDescriptor == DescriptorType.AUC && _aucMap == null) return false;
            if (SelectedDescriptor == DescriptorType.MTT && _mttMap == null) return false;
            if (SelectedDescriptor == DescriptorType.TTP && _ttpMap == null) return false;
            return true;
        });
    }

    public async Task CalculateAllAsync()
    {
        try
        {
            var frames = DicomStorage.Instance.Images;
            var ushortFrames = FramesToUshort(frames);

            double[] timePoints = frames
                .Select(img => img.Dataset.GetSingleValueOrDefault(DicomTag.TriggerTime, -1.0) / 1000.0)
                .ToArray();

            var perfusionResults = await Task.Run(() => CalculatePerfusion(frames, ushortFrames, timePoints));
            var perfusionMapResults = await Task.Run(() => CalculatePerfusionMaps(frames, ushortFrames, timePoints));

            AUCResult = perfusionResults.AUCResult;
            MTTResult = perfusionResults.MTTResult;
            TTPResult = perfusionResults.TTPResult;

            AucMap = perfusionMapResults.AucMap;
            MttMap = perfusionMapResults.MttMap;
            TtpMap = perfusionMapResults.TtpMap;

            BuildPlot(perfusionResults.TimePoints, perfusionResults.ConcentrationPoints);

            DescriptorChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ExportCurrentMapToPng()
    {
        try
        {
            float[,]? mapToExport = SelectedDescriptor switch
            {
                DescriptorType.AUC => _aucMap,
                DescriptorType.MTT => _mttMap,
                DescriptorType.TTP => _ttpMap,
                _ => null
            };

            using var dialog = new FolderBrowserDialog
            {
                Description = "Виберіть папку для збереження карти",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                string fileName = $"perfusion_{SelectedDescriptor.ToString().ToLower()}.png";
                string fullPath = Path.Combine(dialog.SelectedPath, fileName);

                Services.ImageManager.SaveMapAsPng(mapToExport, fullPath, SelectedDescriptor);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Помилка експорту: " + ex.Message;
        }
    }

    private PerfusionResults CalculatePerfusion(List<DicomImage> frames, ushort[][] ushortFrames, double[] timePoints)
    {
        var results = new PerfusionResults();

        int height = frames[0].Height;
        int width = frames[0].Width;

        double[] intensityCurve = new double[frames.Count];
        for (int f = 0; f < frames.Count; f++)
        {
            intensityCurve[f] = ushortFrames[f].Average(p => (double)p);
        }

        int baselineCount = System.Math.Min(3, intensityCurve.Length);
        double S0 = intensityCurve.Take(baselineCount).Average();

        double TE = frames[0].Dataset.GetSingleValueOrDefault(DicomTag.EchoTime, 30.0);
        double TE_seconds = TE / 1000.0;

        double[] concentrationCurve = new double[frames.Count];
        for (int f = 0; f < frames.Count; f++)
        {
            concentrationCurve[f] = -1.0 / TE_seconds * System.Math.Log(intensityCurve[f] / S0);
        }

        double[] filteredCurve = SignalFilter.ApplyGaussianFilter(concentrationCurve);

        var spline = SplineInterpolator.GetSpline(timePoints, filteredCurve);

        var denseTimePoints = GenerateDenseTimePoints(timePoints, 15);
        var interpolatedCurve = SplineInterpolator.InterpolateCurve(spline, denseTimePoints);

        results.AUCResult = PerfusionCalculator.CalculateAUC(denseTimePoints, interpolatedCurve).ToString("F2");
        results.MTTResult = PerfusionCalculator.CalculateMTT(denseTimePoints, interpolatedCurve).ToString("F2");
        results.TTPResult = PerfusionCalculator.CalculateTTP(denseTimePoints, interpolatedCurve).ToString("F2");

        results.TimePoints = denseTimePoints;
        results.ConcentrationPoints = interpolatedCurve;

        return results;
    }

    private PerfusionMapResults CalculatePerfusionMaps(List<DicomImage> frames, ushort[][] ushortFrames, double[] timePoints)
    {
        var results = new PerfusionMapResults();

        int height = frames[0].Height;
        int width = frames[0].Width;

        int baselineCount = System.Math.Min(3, frames.Count);

        double TE = frames[0].Dataset.GetSingleValueOrDefault(DicomTag.EchoTime, 30.0);
        double TE_seconds = TE / 1000.0;

        var denseTimePoints = GenerateDenseTimePoints(timePoints, 15);

        results.AucMap = new float[height, width];
        results.MttMap = new float[height, width];
        results.TtpMap = new float[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double[] pixelCurve = new double[frames.Count];

                for (int f = 0; f < frames.Count; f++)
                {
                    ushort pixelValue = ushortFrames[f][y * width + x];
                    pixelCurve[f] = pixelValue;
                }

                double pixelS0 = pixelCurve.Take(baselineCount).Average();

                double[] mapConcentrationCurve = new double[frames.Count];
                for (int f = 0; f < frames.Count; f++)
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

                var mapSpline = SplineInterpolator.GetSpline(timePoints, filteredMapCurve);

                double[] interpolatedMapCurve = SplineInterpolator.InterpolateCurve(mapSpline, denseTimePoints);

                results.AucMap[y, x] = (float)PerfusionCalculator.CalculateAUC(denseTimePoints, interpolatedMapCurve);
                results.MttMap[y, x] = System.Math.Clamp((float)PerfusionCalculator.CalculateMTT(denseTimePoints, interpolatedMapCurve), 0, 70);
                results.TtpMap[y, x] = (float)PerfusionCalculator.CalculateTTP(denseTimePoints, interpolatedMapCurve);
            }
        }

        return results;
    }

    private void BuildPlot(double[] timePoints, double[] concentrationPoints)
    {
        var plotModel = new PlotModel { Title = "Час – Концентрація" };


        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "t, с"
        });

        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "s(t)"
        });

        var series = new LineSeries
        {
            Color = OxyColors.SkyBlue
        };

        for (int i = 0; i < timePoints.Length; i++)
        {
            series.Points.Add(new DataPoint(timePoints[i], concentrationPoints[i]));
        }

        plotModel.Series.Add(series);

        PerfusionPlotModel = plotModel;
    }

    private ushort[][] FramesToUshort(List<DicomImage> frames)
    {
        int width = frames[0].Width;
        int height = frames[0].Height;

        ushort[][] allFrames = new ushort[frames.Count][];
        for (int f = 0; f < frames.Count; f++)
        {
            var pixelData = DicomPixelData.Create(frames[f].Dataset);
            byte[] rawBytes = pixelData.GetFrame(0).Data;

            int numPixels = width * height;
            ushort[] pixels = new ushort[numPixels];

            for (int i = 0; i < numPixels; i++)
                pixels[i] = BitConverter.ToUInt16(rawBytes, i * 2);

            allFrames[f] = pixels;
        }
        return allFrames;
    }

    public static double[] GenerateDenseTimePoints(double[] timePoints, int stepsPerInterval)
    {
        double min = timePoints.First();
        double max = timePoints.Last();

        double originalStep = (max - min) / (timePoints.Length - 1);
        double interpStep = originalStep / stepsPerInterval;

        int count = (int)System.Math.Round((max - min) / interpStep) + 1;

        double[] dense = new double[count];
        for (int i = 0; i < count; i++)
        {
            dense[i] = min + i * interpStep;
        }

        return dense;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}