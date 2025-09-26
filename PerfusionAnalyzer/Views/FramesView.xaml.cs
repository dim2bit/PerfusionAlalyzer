using OpenTK.Wpf;
using PerfusionAnalyzer.Core.Services;
using PerfusionAnalyzer.ViewModels;
using System.Windows.Input;

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

        _viewModel.FilesLoaded += (_, __) =>
        {
            _viewModel.LoadFrames();
        };

        _viewModel.FrameChanged += (_, __) =>
        {
            if (_viewModel.CurrentDicomFrame != null)
            {
                _renderer.LoadFrameTexture(_viewModel.CurrentDicomFrame);
            }
        };
    }

    private void UserControl_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        int delta = e.Delta > 0 ? -1 : 1;
        int newIndex = _viewModel.CurrentFrameIndex - delta;

        newIndex = System.Math.Max(0, System.Math.Min(_viewModel.MaxFrameIndex, newIndex));
        _viewModel.CurrentFrameIndex = newIndex;

        e.Handled = true;
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