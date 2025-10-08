using Dicom;
using Dicom.Imaging;
using PerfusionAnalyzer.Core.Dicom;

namespace PerfusionAnalyzer.Core.Services;

public class DicomStorage
{
    private static DicomStorage? _instance;
    public static DicomStorage Instance => _instance ??= new DicomStorage();

    private int _sliceIndex;
    private List<List<DicomImage>>? _slices = new();

    private DicomStorage() { }

    public List<List<DicomImage>>? AllSlices => _slices;
    public List<DicomImage>? CurrentSlice => _slices.Count > 0 ? _slices[_sliceIndex] : null;

    public event EventHandler? SliceUpdated;

    public void LoadFrames(List<DicomImage> frames)
    {
        _sliceIndex = 0;
        var orderedFrames = frames.OrderBy(DicomUtils.GetFrameTime).ToList();

        _slices.Clear();

        var groups = orderedFrames.GroupBy(img =>
        {
            var sliceLocation = img.Dataset.Get<double?>(DicomTag.SliceLocation);
            return sliceLocation ?? double.NaN;
        });

        foreach (var group in groups.OrderBy(g => g.Key))
        {
            var slice = new List<DicomImage>(group);
            _slices.Add(slice);
        }
    }

    public void SetSlice(int sliceIndex)
    {
        _sliceIndex = sliceIndex;
        SliceUpdated?.Invoke(this, EventArgs.Empty);
    }
}