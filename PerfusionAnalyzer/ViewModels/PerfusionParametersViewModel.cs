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
using PerfusionAnalyzer.Core.Math;
using PerfusionAnalyzer.Core.Dicom;

namespace PerfusionAnalyzer.ViewModels;

public class PerfusionParametersViewModel : INotifyPropertyChanged
{
    private readonly ProcessingSettings _processingSettings = new();

    private readonly PerfusionMaps _maps = new();
    private readonly PerfusionMaps _originalMaps = new();

    private PerfusionService _perfusionService;

    public ObservableCollection<ComboBoxItem> AvailableCurveTypes { get; } = new ObservableCollection<ComboBoxItem>
    {
        new ComboBoxItem { DisplayName = "Інтенсивність", Value = CurveType.Intensity },
        new ComboBoxItem { DisplayName = "Концентрація", Value = CurveType.Concentration }
    };

    public ObservableCollection<ComboBoxItem> AvailableFilters { get; } = new ObservableCollection<ComboBoxItem>
    {
        new ComboBoxItem { DisplayName = "Не фільтрується", Value = FilterType.None },
        new ComboBoxItem { DisplayName = "Гаусів", Value = FilterType.Gaussian },
        new ComboBoxItem { DisplayName = "Рухоме середнє", Value = FilterType.MovingAverage }
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

    public CurveType SelectedCurveType
    {
        get => _processingSettings.CurveType;
        set
        {
            if (_processingSettings.CurveType != value)
            {
                _processingSettings.CurveType = value;
                OnPropertyChanged(nameof(SelectedFilter));
            }
        }
    }

    public FilterType SelectedFilter
    {
        get => _processingSettings.FilterType;
        set
        {
            if (_processingSettings.FilterType != value)
            {
                _processingSettings.FilterType = value;
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

    private string _mapsCorrelationValue;
    public string MapsCorrelationValue
    {
        get => _mapsCorrelationValue;
        set
        {
            if (_mapsCorrelationValue != value)
            {
                _mapsCorrelationValue = value;
                OnPropertyChanged(nameof(MapsCorrelationValue));
            }
        }
    }

    private string _maskFileName = "* маска не завантажена";
    public string MaskFileName
    {
        get => _maskFileName;
        set
        {
            if (_maskFileName != value)
            {
                _maskFileName = value;
                OnPropertyChanged(nameof(MaskFileName));
            }
        }
    }

    private string _externalMapFileName = "* карта не завантажена";
    public string ExternalMapFileName
    {
        get => _externalMapFileName;
        set
        {
            if (_externalMapFileName != value)
            {
                _externalMapFileName = value;
                OnPropertyChanged(nameof(ExternalMapFileName));
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

    private float[,] _externalMap;
    public float[,] ExternalMap
    {
        get => _externalMap;
        set
        {
            if (_externalMap != value)
            {
                _externalMap = value;
                OnPropertyChanged(nameof(ExternalMap));
            }
        }
    }

    public bool[,]? Mask
    {
        get => _processingSettings.Mask;
        set
        {
            if (_processingSettings.Mask != value)
            {
                _processingSettings.Mask = value;
                OnPropertyChanged(nameof(Mask));
            }
        }
    }

    public double LeakageCoefficient
    {
        get => _processingSettings.LeakageCoefficient;
        set
        {
            if (_processingSettings.LeakageCoefficient != value)
            {
                _processingSettings.LeakageCoefficient = value;
                OnPropertyChanged(nameof(LeakageCoefficient));
            }
        }
    }

    public int ContrastArrivalPercent
    {
        get => _processingSettings.ContrastArrivalPercent;
        set
        {
            if (_processingSettings.ContrastArrivalPercent != value)
            {
                _processingSettings.ContrastArrivalPercent = value;
                OnPropertyChanged(nameof(ContrastArrivalPercent));
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

    public ICommand LoadMaskFileCommand { get; }
    public ICommand LoadMapFileCommand { get; }
    public ICommand ExportMapCommand { get; }

    public PerfusionParametersViewModel()
    {
        LoadMaskFileCommand = new RelayCommand(_ => LoadMaskFile(), _ => IsDataLoaded);
        LoadMapFileCommand = new RelayCommand(_ => LoadMapFile(), _ => IsDataLoaded);
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

            var perfusionMetrics = await _perfusionService.CalculateMetricsAsync(_processingSettings);
            var perfusionMaps = await _perfusionService.CalculateMapsAsync(_processingSettings);

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

            BuildPlot(perfusionMetrics.Time, perfusionMetrics.Curve,
                perfusionMetrics.SlicedTime, perfusionMetrics.SlicedCurve);

            MapsCorrelationValue = GetCorrelation();

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
            if (_perfusionService != null)
            {
                var perfusionMapsPostProcessed = await _perfusionService.PostProcessMapsAsync(_originalMaps, _processingSettings);

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

    private void LoadMaskFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Виберіть файл маски",
            Filter = "Маска (*.txt;*.bin)|*.txt;*.bin|Усі файли (*.*)|*.*"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            string filePath = dialog.FileName;

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                var mask = DicomUtils.ExtractMask(data);

                MaskFileName = Path.GetFileName(filePath);
                Mask = mask;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Помилка при завантаженні маски:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadMapFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Виберіть файл маски",
            Filter = "DICOM Files (*.dcm)|*.dcm"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            string filePath = dialog.FileName;

            try
            {
                var dicomImage = new DicomImage(filePath);
                int width = dicomImage.Width;
                int height = dicomImage.Height;

                if (AUCMap == null || AUCMap.GetLength(0) != height || AUCMap.GetLength(1) != width)
                {
                    System.Windows.MessageBox.Show("Розмір зовнішньої карти не співпадає з внутрішньою AUC картою.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                float[,] externalMap = new float[height, width];
                var pixels = DicomUtils.FrameToUshort(dicomImage, width, height);

                if (pixels == null)
                    throw new Exception("Не вдалося зчитати пікселі з DICOM.");

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        externalMap[y, x] = pixels[y * width + x];
                    }
                }

                ExternalMapFileName = Path.GetFileName(filePath);
                ExternalMap = externalMap;

                MapsCorrelationValue = GetCorrelation();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Помилка при завантаженні карти:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportCurrentMapToPng()
    {
        try
        {
            float[,]? mapToExport = GetCurrentMap();

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

    private void BuildPlot(
        double[] time, double[] curve,
        double[] slicedTime, double[] slicedCurve)
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

        var fullSeries = new LineSeries
        {
            Color = OxyColors.LightBlue,
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };

        for (int i = 0; i < time.Length; i++)
        {
            fullSeries.Points.Add(new DataPoint(time[i], curve[i]));
        }

        var cutSeries = new LineSeries
        {
            Color = OxyColors.Red,
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 2,
            MarkerStroke = OxyColors.DarkRed,
            MarkerFill = OxyColors.White
        };

        for (int i = 0; i < slicedTime.Length; i++)
        {
            cutSeries.Points.Add(new DataPoint(slicedTime[i], slicedCurve[i]));
        }

        plotModel.Series.Add(fullSeries);
        plotModel.Series.Add(cutSeries);

        PerfusionPlotModel = plotModel;
    }

    private string GetCorrelation()
    {
        if (Mask == null || ExternalMap == null)
            return string.Empty;

        var map = GetCurrentMap();
        if (map == null)
            return string.Empty;

        return CorrelationCalculator.CalculatePearson(map, ExternalMap).ToString();
    }

    private float[,]? GetCurrentMap()
    {
        return SelectedDescriptor switch
        {
            DescriptorType.AUC => _originalMaps.AUCMap,
            DescriptorType.MTT => _originalMaps.MTTMap,
            DescriptorType.TTP => _originalMaps.TTPMap,
            _ => null
        };
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}