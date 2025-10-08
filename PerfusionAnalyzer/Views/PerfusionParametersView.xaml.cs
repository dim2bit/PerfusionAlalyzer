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
    private readonly DicomImageRenderer _mapRenderer;
    private readonly DicomImageRenderer _externalMapRenderer;

    private readonly PerfusionParametersViewModel _viewModel;

    private bool _updateMapTexture = false;
    private bool _updateExternalMapTexture = false;

    public PerfusionParametersView()
    {
        InitializeComponent();
        _viewModel = new PerfusionParametersViewModel();
        _mapRenderer = new DicomImageRenderer();
        _externalMapRenderer = new DicomImageRenderer();
        DataContext = _viewModel;

        var settings = new GLWpfControlSettings
        {
            MajorVersion = 4,
            MinorVersion = 2,
            GraphicsProfile = OpenTK.Windowing.Common.ContextProfile.Compatability
        };
        MapOpenTkControl.Start(settings);
        ExternalMapOpenTkControl.Start(settings);

        _viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.SelectedDescriptor))
            {
                _updateMapTexture = true;
            }
            else if (e.PropertyName == nameof(_viewModel.ExternalMap))
            {
                _updateExternalMapTexture = true;
            }
            else if (e.PropertyName == nameof(_viewModel.Mask) ||
                e.PropertyName == nameof(_viewModel.LeakageCoefficient) ||
                e.PropertyName == nameof(_viewModel.SelectedCurveType) ||
                e.PropertyName == nameof(_viewModel.SelectedFilter) ||
                e.PropertyName == nameof(_viewModel.ContrastArrivalPercent))
            {
                var frames = DicomStorage.Instance.CurrentSlice;
                if (frames != null && frames.Count > 0)
                    await _viewModel.InitializeAsync(frames);
            }
            else if (e.PropertyName == nameof(_viewModel.Gamma))
            {
                await _viewModel.PostProcessMapsAsync();
                _updateMapTexture = true;
            }
        };

        DicomStorage.Instance.SliceUpdated += async (_, __) =>
        {
            var frames = DicomStorage.Instance.CurrentSlice;
            if (frames != null && frames.Count > 0)
                await _viewModel.InitializeAsync(frames);
        };
    }

    private void MapOpenTkControl_OnRender(TimeSpan delta)
    {
        if (_mapRenderer != null)
        {
            if (_updateMapTexture)
            {
                UpdateMapTexture();
                _updateMapTexture = false;
            }
            int controlWidth = (int)MapOpenTkControl.Width;
            int controlHeight = (int)MapOpenTkControl.Height;

            _mapRenderer.Render(controlWidth, controlHeight);
        }
    }

    private void ExternalMapOpenTkControl_OnRender(TimeSpan delta)
    {
        if (_externalMapRenderer != null)
        {
            if (_updateExternalMapTexture)
            {
                UpdateExternalMapTexture();
                _updateExternalMapTexture = false;
            }
            int controlWidth = (int)ExternalMapOpenTkControl.Width;
            int controlHeight = (int)ExternalMapOpenTkControl.Height;

            _externalMapRenderer.Render(controlWidth, controlHeight);
        }
    }

    private void UpdateMapTexture()
    {
        switch (_viewModel.SelectedDescriptor)
        {
            case DescriptorType.AUC:
                if (_viewModel.AUCMap != null)
                    _mapRenderer.LoadMapTextureColored(_viewModel.AUCMap);
                break;
            case DescriptorType.MTT:
                if (_viewModel.MTTMap != null)
                    _mapRenderer.LoadMapTextureColored(_viewModel.MTTMap);
                break;
            case DescriptorType.TTP:
                if (_viewModel.TTPMap != null)
                    _mapRenderer.LoadMapTextureColored(_viewModel.TTPMap);
                break;
        }
    }

    private void UpdateExternalMapTexture()
    {
        _externalMapRenderer.LoadMapTextureColored(_viewModel.ExternalMap);
    }
}