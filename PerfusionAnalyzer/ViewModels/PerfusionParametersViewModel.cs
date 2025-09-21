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
        var results = await Task.Run(CalculatePerfusion);

        ErrorMessage = results.ErrorMessage;
        AUCResult = results.AUCResult;
        MTTResult = results.MTTResult;
        TTPResult = results.TTPResult;
        _aucMap = results.AucMap;
        _mttMap = results.MttMap;
        _ttpMap = results.TtpMap;

        if (results.ErrorMessage == "")
        {
            BuildPlot(results.TimePoints, results.FilteredCurve);
            DescriptorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ExportCurrentMapToPng()
    {
        try
        {
            float[,] mapToExport = SelectedDescriptor switch
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

    private PerfusionResults CalculatePerfusion()
    {
        var results = new PerfusionResults();

        try
        {
            results.ErrorMessage = "";

            var frames = DicomStorage.Instance.Images;

            results.TimePoints = frames
                .Select(img => img.Dataset.GetSingleValueOrDefault(DicomTag.TriggerTime, -1.0) / 1000.0)
                .ToArray();

            int height = frames[0].Height;
            int width = frames[0].Width;

            ushort[][] allFrames = new ushort[frames.Count][];

            for (int f = 0; f < frames.Count; f++)
            {
                var pixelData = DicomPixelData.Create(frames[f].Dataset);
                byte[] rawBytes = pixelData.GetFrame(0).Data;

                int numPixels = width * height;
                ushort[] pixels = new ushort[numPixels];

                for (int i = 0; i < numPixels; i++)
                {
                    pixels[i] = BitConverter.ToUInt16(rawBytes, i * 2);
                }

                allFrames[f] = pixels;
            }

            double[] concentrationCurve = new double[frames.Count];
            for (int f = 0; f < frames.Count; f++)
            {
                concentrationCurve[f] = allFrames[f].Average(p => (double)p);
            }

            results.FilteredCurve = SignalFilter.ApplyGaussianFilter(concentrationCurve, radius: 2, sigma: 1.0);

            results.AUCResult = PerfusionCalculator.CalculateAUC(results.TimePoints, results.FilteredCurve).ToString("F2");
            results.MTTResult = PerfusionCalculator.CalculateMTT(results.TimePoints, results.FilteredCurve).ToString("F2");
            results.TTPResult = PerfusionCalculator.CalculateTTP(results.TimePoints, results.FilteredCurve).ToString("F2");

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
                        ushort pixelValue = allFrames[f][y * width + x];
                        pixelCurve[f] = pixelValue;
                    }

                    double[] filtered = SignalFilter.ApplyGaussianFilter(pixelCurve, radius: 2, sigma: 1.0);

                    results.AucMap[y, x] = (float)PerfusionCalculator.CalculateAUC(results.TimePoints, filtered);
                    results.MttMap[y, x] = (float)PerfusionCalculator.CalculateMTT(results.TimePoints, filtered);
                    results.TtpMap[y, x] = (float)PerfusionCalculator.CalculateTTP(results.TimePoints, filtered);
                }
            }
        }
        catch (Exception ex)
        {
            results.ErrorMessage = ex.Message;
        }

        return results;
    }

    private void BuildPlot(double[] timePoints, double[] curvePoints)
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
            MarkerType = MarkerType.Circle,
            MarkerSize = 1,
            MarkerStroke = OxyColors.DarkBlue,
            Color = OxyColors.SkyBlue
        };

        for (int i = 0; i < timePoints.Length; i++)
        {
            series.Points.Add(new DataPoint(timePoints[i], curvePoints[i]));
        }

        plotModel.Series.Add(series);

        PerfusionPlotModel = plotModel;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}