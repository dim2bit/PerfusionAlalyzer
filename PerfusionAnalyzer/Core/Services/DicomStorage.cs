using Dicom;
using Dicom.Imaging;

namespace PerfusionAnalyzer.Core.Services;

public class DicomStorage
{
    private static DicomStorage? _instance;
    public static DicomStorage Instance => _instance ??= new DicomStorage();

    private List<DicomImage>? _dicomImages;

    private DicomStorage() { }

    public List<DicomImage>? Images => _dicomImages;

    public event EventHandler? ImagesUpdated;

    public void SetImages(List<DicomImage> images)
    {
        _dicomImages = images
            .OrderBy(image => image.Dataset.GetSingleValueOrDefault(DicomTag.TriggerTime, 0.0))
            .ToList();
        ImagesUpdated?.Invoke(this, EventArgs.Empty);
    }
}