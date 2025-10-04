using OpenTK.Wpf;
using PerfusionAnalyzer.Core.Services;
using PerfusionAnalyzer.Models;
using PerfusionAnalyzer.ViewModels;

namespace PerfusionAnalyzer.Views;

/// <summary>
/// Interaction logic for PerfusionParametersView.xaml
/// </summary>
public partial class PerfusionParametersView : System.Windows.Controls.UserControl
{
    private readonly DicomImageRenderer _renderer;
    private readonly PerfusionParametersViewModel _viewModel;

    private bool _needUpdateTexture = false;

    public PerfusionParametersView()
    {
        InitializeComponent();
        _viewModel = new PerfusionParametersViewModel();
        _renderer = new DicomImageRenderer();
        DataContext = _viewModel;

        var settings = new GLWpfControlSettings
        {
            MajorVersion = 4,
            MinorVersion = 2,
            GraphicsProfile = OpenTK.Windowing.Common.ContextProfile.Compatability
        };
        OpenTkControl.Start(settings);

        _viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.SelectedDescriptor))
            {
                _needUpdateTexture = true;
            }
            else if (e.PropertyName == nameof(_viewModel.SelectedFilter) ||
                e.PropertyName == nameof(_viewModel.ContrastArrivalPercent))
            {
                var frames = DicomStorage.Instance.CurrentSlice;
                if (frames != null && frames.Count > 0)
                    await _viewModel.InitializeAsync(frames);
            }
            else if (e.PropertyName == nameof(_viewModel.Gamma) ||
                e.PropertyName == nameof(_viewModel.BackgroundThreshold))
            {
                await _viewModel.PostProcessMapsAsync();
                _needUpdateTexture = true;
            }
        };

        DicomStorage.Instance.SliceUpdated += async (_, __) =>
        {
            var frames = DicomStorage.Instance.CurrentSlice;
            if (frames != null && frames.Count > 0)
                await _viewModel.InitializeAsync(frames);
        };
    }

    private void OpenTkControl_OnRender(TimeSpan delta)
    {
        if (_renderer != null)
        {
            if (_needUpdateTexture)
            {
                UpdateTexture();
                _needUpdateTexture = false;
            }
            int controlWidth = (int)OpenTkControl.Width;
            int controlHeight = (int)OpenTkControl.Height;

            _renderer.Render(controlWidth, controlHeight);
        }
    }

    private void UpdateTexture()
    {
        switch (_viewModel.SelectedDescriptor)
        {
            case DescriptorType.AUC:
                if (_viewModel.AUCMap != null)
                    _renderer.LoadMapTextureColored(_viewModel.AUCMap, DescriptorType.AUC);
                break;
            case DescriptorType.MTT:
                if (_viewModel.MTTMap != null)
                    _renderer.LoadMapTextureColored(_viewModel.MTTMap, DescriptorType.MTT);
                break;
            case DescriptorType.TTP:
                if (_viewModel.TTPMap != null)
                    _renderer.LoadMapTextureColored(_viewModel.TTPMap, DescriptorType.TTP);
                break;
        }
    }
}