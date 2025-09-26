using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PerfusionAnalyzer.Core.Services;
using PerfusionAnalyzer.Models;
using PerfusionAnalyzer.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PerfusionAnalyzer.Core.Utils;
using System.IO;

namespace PerfusionAnalyzer.ViewModels;

public class PerfusionParametersViewModel : INotifyPropertyChanged
{
    private readonly ProcessingSettings _processingSettings = new();

    private PerfusionMaps _originalMaps;

    private PerfusionService _perfusionService;

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

    private DescriptorType _selectedDescriptor;
    public DescriptorType SelectedDescriptor
    {
        get => _selectedDescriptor;
        set
        {
            if (_selectedDescriptor != value)
            {
                _selectedDescriptor = value;
                OnPropertyChanged(nameof(SelectedDescriptor));
            }
        }
    }

    private string _aucResult = "";
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

    private string _mttResult = "";
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

    private string _ttpResult = "";
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

    private float[,] _aucMap;
    public float[,] AUCMap
    {
        get => _aucMap;
        set
        {
            if (_aucMap != value)
            {
                _aucMap = value;
                OnPropertyChanged(nameof(AUCMap));
            }
        }
    }

    private float[,] _mttMap;
    public float[,] MTTMap
    {
        get => _mttMap;
        set
        {
            if (_mttMap != value)
            {
                _mttMap = value;
                OnPropertyChanged(nameof(MTTMap));
            }
        }
    }

    private float[,] _ttpMap;
    public float[,] TTPMap
    {
        get => _ttpMap;
        set
        {
            if (_ttpMap != value)
            {
                _ttpMap = value;
                OnPropertyChanged(nameof(TTPMap));
            }
        }
    }

    public double Gamma
    {
        get => _processingSettings.Gamma;
        set
        {
            if (_processingSettings.Gamma != value)
            {
                _processingSettings.Gamma = value;
                OnPropertyChanged(nameof(Gamma));
            }
        }
    }

    public int KernelSize
    {
        get => _processingSettings.KernelSize;
        set
        {
            if (_processingSettings.KernelSize != value)
            {
                _processingSettings.KernelSize = value;
                OnPropertyChanged(nameof(KernelSize));
            }
        }
    }

    public ushort Threshold
    {
        get => _processingSettings.Threshold;
        set
        {
            if (_processingSettings.Threshold != value)
            {
                _processingSettings.Threshold = value;
                OnPropertyChanged(nameof(Threshold));
            }
        }
    }

    public bool IsPostProcessingEnabled
    {
        get => _processingSettings.IsPostProcessingEnabled;
        set
        {
            if (_processingSettings.IsPostProcessingEnabled != value)
            {
                _processingSettings.IsPostProcessingEnabled = value;
                OnPropertyChanged(nameof(IsPostProcessingEnabled));
            }
        }
    }

    private bool _isDataLoaded = false;
    public bool IsDataLoaded
    {
        get => _isDataLoaded;
        set
        {
            if (_isDataLoaded != value)
            {
                _isDataLoaded = value;
                OnPropertyChanged(nameof(IsDataLoaded));
            }
        }
    }

    private string _errorMessage = "";
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
            if (SelectedDescriptor == DescriptorType.AUC && AUCMap == null) return false;
            if (SelectedDescriptor == DescriptorType.MTT && MTTMap == null) return false;
            if (SelectedDescriptor == DescriptorType.TTP && TTPMap == null) return false;
            return true;
        });
    }

    public async Task CalculateAllAsync()
    {
        try
        {
            var frames = DicomStorage.Instance.Images;
            _perfusionService = new PerfusionService(frames);

            var perfusionMetrics = await _perfusionService.CalculateMetricsAsync();
            var perfusionMaps = await _perfusionService.CalculateMapsAsync();

            AUCResult = perfusionMetrics.AUCResult;
            MTTResult = perfusionMetrics.MTTResult;
            TTPResult = perfusionMetrics.TTPResult;

            AUCMap = perfusionMaps.AUCMap;
            MTTMap = perfusionMaps.MTTMap;
            TTPMap = perfusionMaps.TTPMap;

            _originalMaps = new PerfusionMaps
            {
                AUCMap = (float[,])AUCMap.Clone(),
                MTTMap = (float[,])MTTMap.Clone(),
                TTPMap = (float[,])TTPMap.Clone()
            };

            await ReprocessMapsAsync();

            BuildPlot(perfusionMetrics.TimePoints, perfusionMetrics.ConcentrationPoints);

            IsDataLoaded = true;

            OnPropertyChanged(nameof(SelectedDescriptor));
        }
        catch (InvalidOperationException ex)
        {
            System.Windows.MessageBox.Show($"{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Помилка при розрахунку перфузійних параметрів:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task ReprocessMapsAsync()
    {
        try
        {
            if (!IsPostProcessingEnabled)
            {
                AUCMap = _originalMaps.AUCMap;
                MTTMap = _originalMaps.MTTMap;
                TTPMap = _originalMaps.TTPMap;
                return;
            }

            if (_perfusionService != null)
            {
                var perfusionMapsPostProcessed = await _perfusionService.PostProcessMapsAsync(FilterType.Median, _originalMaps, Threshold, KernelSize, Gamma);

                AUCMap = perfusionMapsPostProcessed.AUCMap;
                MTTMap = perfusionMapsPostProcessed.MTTMap;
                TTPMap = perfusionMapsPostProcessed.TTPMap;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Помилка при постобробці карт:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportCurrentMapToPng()
    {
        try
        {
            float[,]? mapToExport = SelectedDescriptor switch
            {
                DescriptorType.AUC => AUCMap,
                DescriptorType.MTT => MTTMap,
                DescriptorType.TTP => TTPMap,
                _ => null
            };

            if (mapToExport != null)
            {
                using var dialog = new SaveFileDialog
                {
                    Title = "Збереження карти перфузії",
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
                    FileName = $"perfusion_{SelectedDescriptor.ToString().ToLower()}",
                    DefaultExt = "png",
                    AddExtension = true
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var format = Path.GetExtension(dialog.FileName)?.ToLower() switch
                    {
                        ".jpg" or ".jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
                        ".bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
                        _ => System.Drawing.Imaging.ImageFormat.Png
                    };
                    ImageUtils.SaveMapAsImage(SelectedDescriptor, mapToExport, dialog.FileName, format);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Помилка при експорті карти:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}