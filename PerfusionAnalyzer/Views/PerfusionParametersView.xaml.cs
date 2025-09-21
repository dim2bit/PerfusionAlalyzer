using OpenTK.Wpf;
using PerfusionAnalyzer.Models;
using PerfusionAnalyzer.Services;
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

        _viewModel.DescriptorChanged += (_, __) =>
        {
            _needUpdateTexture = true;
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
                if (_viewModel.AucMap != null)
                    _renderer.LoadMapTextureColored(_viewModel.AucMap, DescriptorType.AUC);
                break;
            case DescriptorType.MTT:
                if (_viewModel.MttMap != null)
                    _renderer.LoadMapTextureColored(_viewModel.MttMap, DescriptorType.MTT);
                break;
            case DescriptorType.TTP:
                if (_viewModel.TtpMap != null)
                    _renderer.LoadMapTextureColored(_viewModel.TtpMap, DescriptorType.TTP);
                break;
        }
    }
}