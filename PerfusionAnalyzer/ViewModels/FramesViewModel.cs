using Dicom;
using Dicom.Imaging;
using PerfusionAnalyzer.Core.Services;
using PerfusionAnalyzer.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PerfusionAnalyzer.Core.Dicom;

namespace PerfusionAnalyzer.ViewModels;

public class FramesViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "Завантажте файли";
    private int _currentIndex = 0;
    private ObservableCollection<DicomImage> _dicomFrames = new();

    private double[] _timePoints;

    public event EventHandler? FilesLoaded;
    public event EventHandler? FrameChanged;

    public FramesViewModel()
    {
        LoadDicomFilesCommand = new RelayCommand(_ => LoadDicomFiles());
        NextFrameCommand = new RelayCommand(_ => NextFrame(), _ => _dicomFrames.Count > 1);
        PrevFrameCommand = new RelayCommand(_ => PrevFrame(), _ => _dicomFrames.Count > 1);
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
        _dicomFrames.Count > 0 ? _dicomFrames[_currentIndex] : null;

    public string FrameInfo =>
        _dicomFrames.Count > 0 ? $"Кадр {_currentIndex + 1} із {_dicomFrames.Count}" : "Немає кадрів";

    public int MaxFrameIndex => _dicomFrames.Count > 0 ? _dicomFrames.Count - 1 : 0;

    public string CurrentFrameTimeDisplay =>
        (_timePoints != null && !_timePoints.Any(t => t < 0) && CurrentFrameIndex >= 0 && CurrentFrameIndex < _timePoints.Length)
            ? $"Час: {_timePoints[CurrentFrameIndex]:F2} с"
            : "";

    public int CurrentFrameIndex
    {
        get => _currentIndex;
        set
        {
            if (value != _currentIndex && value >= 0 && value < _dicomFrames.Count)
            {
                _currentIndex = value;
                OnPropertyChanged(nameof(CurrentFrameIndex));
                OnPropertyChanged(nameof(CurrentDicomFrame));
                OnPropertyChanged(nameof(FrameInfo));
                OnPropertyChanged(nameof(CurrentFrameTimeDisplay));
                FrameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ICommand LoadDicomFilesCommand { get; }
    public ICommand NextFrameCommand { get; }
    public ICommand PrevFrameCommand { get; }

    public void LoadFrames()
    {
        var images = DicomStorage.Instance.Images;

        if (images == null || images.Count == 0)
            return;

        _dicomFrames = new ObservableCollection<DicomImage>(images);
        _timePoints = DicomUtils.GetTimePoints(images);
        _currentIndex = 0;

        OnPropertyChanged(nameof(CurrentDicomFrame));
        OnPropertyChanged(nameof(FrameInfo));
        OnPropertyChanged(nameof(CurrentFrameIndex));
        OnPropertyChanged(nameof(MaxFrameIndex));
        OnPropertyChanged(nameof(CurrentFrameTimeDisplay));

        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadDicomFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "DICOM Files (*.dcm)|*.dcm",
            Title = "Виберіть DICOM-файл",
            Multiselect = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            string[] fileNames = dialog.FileNames;

            try
            {
                var images = DicomUtils.LoadDicomImages(fileNames);

                StatusMessage = $"Завантажено файлів: {images.Count}";

                DicomStorage.Instance.SetImages(images);

                FilesLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Помилка при завантаженні файлів:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void NextFrame()
    {
        if (_currentIndex < _dicomFrames.Count - 1)
        {
            _currentIndex++;
            OnPropertyChanged(nameof(CurrentFrameIndex));
            OnPropertyChanged(nameof(CurrentDicomFrame));
            OnPropertyChanged(nameof(FrameInfo));
            OnPropertyChanged(nameof(CurrentFrameTimeDisplay));
            FrameChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void PrevFrame()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            OnPropertyChanged(nameof(CurrentFrameIndex));
            OnPropertyChanged(nameof(CurrentDicomFrame));
            OnPropertyChanged(nameof(FrameInfo));
            OnPropertyChanged(nameof(CurrentFrameTimeDisplay));
            FrameChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}