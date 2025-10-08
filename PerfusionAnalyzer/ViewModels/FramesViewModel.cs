using Dicom.Imaging;
using PerfusionAnalyzer.Core.Services;
using PerfusionAnalyzer.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PerfusionAnalyzer.Core.Dicom;
using Dicom;

namespace PerfusionAnalyzer.ViewModels;

public class FramesViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "Завантажте файли";

    private int _currentFrameIndex = 0;
    private int _currentSliceIndex = 0;

    private ObservableCollection<ObservableCollection<DicomImage>> _slices = new();

    private double[] _time;

    public event EventHandler? FilesLoaded;
    public event EventHandler? FrameChanged;

    public FramesViewModel()
    {
        LoadDicomFilesCommand = new RelayCommand(_ => LoadDicomFiles());
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
    }

    public DicomImage? CurrentDicomFrame =>
        _slices.Count > 0 && _slices[_currentSliceIndex].Count > 0 ? _slices[_currentSliceIndex][_currentFrameIndex] : null;

    public string FrameInfo => _slices.Count > 0 && _slices[_currentSliceIndex].Count > 0 
        ? $"Кадр {_currentFrameIndex + 1} із {_slices[_currentSliceIndex].Count}" 
        : "Немає кадрів";

    public string SliceInfo => _slices.Count > 0
        ? $"Зріз {_currentSliceIndex + 1} із {_slices.Count}"
        : "Немає зрізів";

    public int MaxFrameIndex => _slices.Count > 0 && _slices[_currentSliceIndex].Count > 0 ? _slices[_currentSliceIndex].Count - 1 : 0;
    public int MaxSliceIndex => _slices.Count > 0 ? _slices.Count - 1 : 0;

    public string CurrentFrameTimeDisplay =>
        (_time != null && !_time.Any(t => t < 0) && CurrentFrameIndex >= 0 && CurrentFrameIndex < _time.Length)
            ? $"Час: {_time[CurrentFrameIndex]:F2}"
            : "";

    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        set
        {
            if (value != _currentFrameIndex && value >= 0 && value < _slices[_currentSliceIndex].Count)
            {
                _currentFrameIndex = value;
                OnPropertyChanged(nameof(CurrentFrameIndex));
                OnPropertyChanged(nameof(CurrentSliceIndex));
                OnPropertyChanged(nameof(CurrentDicomFrame));
                OnPropertyChanged(nameof(FrameInfo));
                OnPropertyChanged(nameof(SliceInfo));
                OnPropertyChanged(nameof(MaxFrameIndex));
                OnPropertyChanged(nameof(MaxSliceIndex));
                OnPropertyChanged(nameof(CurrentFrameTimeDisplay));
                FrameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public int CurrentSliceIndex
    {
        get => _currentSliceIndex;
        set
        {
            if (value != _currentSliceIndex && value >= 0 && value < _slices.Count)
            {
                _currentSliceIndex = value;
                _currentFrameIndex = 0;
                _time = DicomUtils.GetTimePoints(_slices[_currentSliceIndex].ToList());
                DicomStorage.Instance.SetSlice(_currentSliceIndex);

                OnPropertyChanged(nameof(CurrentFrameIndex));
                OnPropertyChanged(nameof(CurrentSliceIndex));
                OnPropertyChanged(nameof(CurrentDicomFrame));
                OnPropertyChanged(nameof(FrameInfo));
                OnPropertyChanged(nameof(SliceInfo));
                OnPropertyChanged(nameof(MaxFrameIndex));
                OnPropertyChanged(nameof(MaxSliceIndex));
                OnPropertyChanged(nameof(CurrentFrameTimeDisplay));
                FrameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ICommand LoadDicomFilesCommand { get; }

    public void LoadFrames()
    {
        _slices = new ObservableCollection<ObservableCollection<DicomImage>>(
            DicomStorage.Instance.AllSlices!.Select(sliceList => new ObservableCollection<DicomImage>(sliceList))
        );

        if (_slices == null || _slices.Count == 0)
            return;

        _currentFrameIndex = 0;
        _currentSliceIndex = 0;

        _time = DicomUtils.GetTimePoints(_slices[_currentSliceIndex].ToList());
        DicomStorage.Instance.SetSlice(_currentSliceIndex);

        OnPropertyChanged(nameof(CurrentFrameIndex));
        OnPropertyChanged(nameof(CurrentSliceIndex));
        OnPropertyChanged(nameof(CurrentDicomFrame));
        OnPropertyChanged(nameof(FrameInfo));
        OnPropertyChanged(nameof(SliceInfo));
        OnPropertyChanged(nameof(MaxFrameIndex));
        OnPropertyChanged(nameof(MaxSliceIndex));
        OnPropertyChanged(nameof(CurrentFrameTimeDisplay));

        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadDicomFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Виберіть DICOM-файл",
            Filter = "DICOM Files (*.dcm)|*.dcm",
            Multiselect = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            string[] fileNames = dialog.FileNames;

            try
            {
                var images = DicomUtils.LoadDicomImages(fileNames);

                StatusMessage = $"Завантажено файлів: {images.Count}";

                DicomStorage.Instance.LoadFrames(images);

                FilesLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Помилка при завантаженні файлів:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}