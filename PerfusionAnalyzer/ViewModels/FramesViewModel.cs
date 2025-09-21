using Dicom;
using Dicom.Imaging;
using PerfusionAnalyzer.Services;
using PerfusionAnalyzer.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace PerfusionAnalyzer.ViewModels;

public class FramesViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "Завантажте файли";
    private int _currentIndex = 0;
    private ObservableCollection<DicomImage> _dicomFrames = new();

    public event EventHandler? FrameChanged;

    public FramesViewModel()
    {
        LoadDicomFileCommand = new RelayCommand(_ => LoadDicomFile());
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
        (CurrentFrameIndex >= 0 && CurrentFrameIndex < _dicomFrames.Count)
            ? $"Час: {_dicomFrames[CurrentFrameIndex].Dataset.GetSingleValueOrDefault(DicomTag.TriggerTime, 0.0) / 1000:F2} с"
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

    public ICommand LoadDicomFileCommand { get; }
    public ICommand NextFrameCommand { get; }
    public ICommand PrevFrameCommand { get; }

    private void LoadFrames(List<DicomImage> images)
    {
        _dicomFrames = new ObservableCollection<DicomImage>();

        for (int i = 0; i < images.Count; i++)
        {
            _dicomFrames.Add(images[i]);
        }

        _currentIndex = 0;

        OnPropertyChanged(nameof(CurrentDicomFrame));
        OnPropertyChanged(nameof(FrameInfo));
        OnPropertyChanged(nameof(CurrentFrameIndex));
        OnPropertyChanged(nameof(MaxFrameIndex));
        OnPropertyChanged(nameof(CurrentFrameTimeDisplay));
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadDicomFile()
    {
        using (var dialog = new OpenFileDialog())
        {
            dialog.Filter = "DICOM Files (*.dcm)|*.dcm";
            dialog.Title = "Виберіть DICOM-файл";
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string[] fileNames = dialog.FileNames;

                try
                {
                    var images = new List<DicomImage>();
                    foreach (string filePath in fileNames)
                    {
                        images.Add(new DicomImage(filePath));
                    }

                    StatusMessage = $"Завантажено файлів: {images.Count}";

                    DicomStorage.Instance.SetImages(images);

                    LoadFrames(DicomStorage.Instance.Images);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Помилка при завантаженні файлів:\n{ex.Message}";
                }
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