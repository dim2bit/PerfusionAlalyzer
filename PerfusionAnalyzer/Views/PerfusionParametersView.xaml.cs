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
        };
        OpenTkControl.Start(settings);

        _viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.SelectedDescriptor))
            {
                _needUpdateTexture = true;
            }
            else if (e.PropertyName == nameof(_viewModel.IsPostProcessingEnabled) ||
                e.PropertyName == nameof(_viewModel.Gamma) ||
                e.PropertyName == nameof(_viewModel.Threshold) ||
                e.PropertyName == nameof(_viewModel.KernelSize))
            {
                _needUpdateTexture = true;
                await _viewModel.ReprocessMapsAsync();
            }
        };

        DicomStorage.Instance.ImagesUpdated += async (_, __) =>
        {
            await _viewModel.CalculateAllAsync();
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
            int controlWidth = (int)OpenTkControl.ActualWidth;
            int controlHeight = (int)OpenTkControl.ActualHeight;

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