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
using Dicom.Imaging;

namespace PerfusionAnalyzer.ViewModels;

public class PerfusionParametersViewModel : INotifyPropertyChanged
{
    private readonly PostProcessingSettings _postProcessingSettings = new();

    private readonly PerfusionMaps _maps = new();
    private readonly PerfusionMaps _originalMaps = new();

    private PerfusionService _perfusionService;

    public ObservableCollection<ComboBoxItem> AvailableFilters { get; } = new ObservableCollection<ComboBoxItem>
    {
        new ComboBoxItem { DisplayName = "Не фільтрується", Value = FilterType.None },
        new ComboBoxItem { DisplayName = "Медіанний", Value = FilterType.Median },
        new ComboBoxItem { DisplayName = "Гаусовий", Value = FilterType.Gaussian },
        new ComboBoxItem { DisplayName = "Білатеральний", Value = FilterType.Bilateral }
    };

    public ObservableCollection<DescriptorType> AvailableDescriptors { get; } = new ObservableCollection<DescriptorType> 
    { 
        DescriptorType.AUC, 
        DescriptorType.MTT, 
        DescriptorType.TTP 
    };

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

    public FilterType SelectedFilter
    {
        get => _postProcessingSettings.FilterType;
        set
        {
            if (_postProcessingSettings.FilterType != value)
            {
                _postProcessingSettings.FilterType = value;
                OnPropertyChanged(nameof(SelectedFilter));
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

    private string _aucValue;
    public string AUCValue
    {
        get => _aucValue;
        set
        {
            if (_aucValue != value)
            {
                _aucValue = value;
                OnPropertyChanged(nameof(AUCValue));
            }
        }
    }

    private string _mttValue;
    public string MTTValue
    {
        get => _mttValue;
        set
        {
            if (_mttValue != value)
            {
                _mttValue = value;
                OnPropertyChanged(nameof(MTTValue));
            }
        }
    }

    private string _ttpValue;
    public string TTPValue
    {
        get => _ttpValue;
        set
        {
            if (_ttpValue != value)
            {
                _ttpValue = value;
                OnPropertyChanged(nameof(TTPValue));
            }
        }
    }

    public float[,] AUCMap
    {
        get => _maps.AUCMap;
        set
        {
            if (_maps.AUCMap != value)
            {
                _maps.AUCMap = value;
                OnPropertyChanged(nameof(AUCMap));
            }
        }
    }

    public float[,] MTTMap
    {
        get => _maps.MTTMap;
        set
        {
            if (_maps.MTTMap != value)
            {
                _maps.MTTMap = value;
                OnPropertyChanged(nameof(MTTMap));
            }
        }
    }

    public float[,] TTPMap
    {
        get => _maps.TTPMap;
        set
        {
            if (_maps.TTPMap != value)
            {
                _maps.TTPMap = value;
                OnPropertyChanged(nameof(TTPMap));
            }
        }
    }

    public bool IsPostProcessingEnabled
    {
        get => _postProcessingSettings.IsEnabled;
        set
        {
            if (_postProcessingSettings.IsEnabled != value)
            {
                _postProcessingSettings.IsEnabled = value;
                OnPropertyChanged(nameof(IsPostProcessingEnabled));
            }
        }
    }

    public double Gamma
    {
        get => _postProcessingSettings.Gamma;
        set
        {
            if (_postProcessingSettings.Gamma != value)
            {
                _postProcessingSettings.Gamma = value;
                OnPropertyChanged(nameof(Gamma));
            }
        }
    }

    public int KernelSize
    {
        get => _postProcessingSettings.KernelSize;
        set
        {
            if (_postProcessingSettings.KernelSize != value)
            {
                _postProcessingSettings.KernelSize = value;
                OnPropertyChanged(nameof(KernelSize));
            }
        }
    }

    public ushort Threshold
    {
        get => _postProcessingSettings.Threshold;
        set
        {
            if (_postProcessingSettings.Threshold != value)
            {
                _postProcessingSettings.Threshold = value;
                OnPropertyChanged(nameof(Threshold));
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

    public async Task InitializeAsync(List<DicomImage> frames)
    {
        try
        {
            _perfusionService = new PerfusionService(frames);

            var perfusionMetrics = await _perfusionService.CalculateMetricsAsync();
            var perfusionMaps = await _perfusionService.CalculateMapsAsync();

            AUCValue = perfusionMetrics.AUC.ToString("F4");
            MTTValue = perfusionMetrics.MTT.ToString("F4");
            TTPValue = perfusionMetrics.TTP.ToString("F4");

            AUCMap = perfusionMaps.AUCMap;
            MTTMap = perfusionMaps.MTTMap;
            TTPMap = perfusionMaps.TTPMap;

            _originalMaps.AUCMap = (float[,])AUCMap.Clone();
            _originalMaps.MTTMap = (float[,])MTTMap.Clone();
            _originalMaps.TTPMap = (float[,])TTPMap.Clone();

            await PostProcessMapsAsync();

            BuildPlot(perfusionMetrics.TimePoints, perfusionMetrics.ConcentrationPoints);

            IsDataLoaded = true;

            OnPropertyChanged(nameof(SelectedDescriptor));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Помилка при розрахунку перфузійних параметрів:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task PostProcessMapsAsync()
    {
        try
        {
            if (!IsPostProcessingEnabled)
            {
                AUCMap = (float[,])_originalMaps.AUCMap.Clone();
                MTTMap = (float[,])_originalMaps.MTTMap.Clone();
                TTPMap = (float[,])_originalMaps.TTPMap.Clone();
                return;
            }

            if (_perfusionService != null)
            {
                var perfusionMapsPostProcessed = await _perfusionService.PostProcessMapsAsync(_originalMaps, _postProcessingSettings);

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
        var plotModel = new PlotModel 
        {
            Title = "Час – Концентрація"
        };

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