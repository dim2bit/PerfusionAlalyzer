using Dicom.Imaging;
using PerfusionAnalyzer.Core.Dicom;

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
        _dicomImages = images.OrderBy(DicomUtils.GetTriggerTime).ToList();
        ImagesUpdated?.Invoke(this, EventArgs.Empty);
    }
}