using OpenTK.Wpf;
using PerfusionAnalyzer.Services;
using PerfusionAnalyzer.ViewModels;

namespace PerfusionAnalyzer.Views;

/// <summary>
/// Interaction logic for FramesView.xaml
/// </summary>
public partial class FramesView : System.Windows.Controls.UserControl
{
    private readonly DicomImageRenderer _renderer;
    private readonly FramesViewModel _viewModel;

    public FramesView()
    {
        InitializeComponent();
        _viewModel = new FramesViewModel();
        _renderer = new DicomImageRenderer();
        DataContext = _viewModel;

        var settings = new GLWpfControlSettings
        {
            MajorVersion = 4,
            MinorVersion = 2,
        };
        OpenTkControl.Start(settings);

        _viewModel.FrameChanged += (_, __) =>
        {
            if (_viewModel.CurrentDicomFrame != null)
            {
                _renderer.LoadFrameTexture(_viewModel.CurrentDicomFrame);
            }
        };
    }

    private void OpenTkControl_OnRender(TimeSpan delta)
    {
        if (_renderer != null)
        {
            int controlWidth = (int)OpenTkControl.ActualWidth;
            int controlHeight = (int)OpenTkControl.ActualHeight;

            _renderer.Render(controlWidth, controlHeight);
        }
    }
}