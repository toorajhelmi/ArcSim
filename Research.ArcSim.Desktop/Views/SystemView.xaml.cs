using Research.ArcSim.Desktop.ViewModels;

namespace Research.ArcSim.Desktop.Views;

public partial class SystemView : ContentPage
{
	public SystemView()
	{
		InitializeComponent();

		BindingContext = new SimulationViewModel();

    }

    private void Deployment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        (BindingContext as SimulationViewModel).SelectedDeploymentOptions.Clear();
        (BindingContext as SimulationViewModel).SelectedDeploymentOptions.AddRange(e.CurrentSelection.Select(i => (string)i));
    }

    private void Processing_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        (BindingContext as SimulationViewModel).SelectedProcessingOptions.Clear();
        (BindingContext as SimulationViewModel).SelectedProcessingOptions.AddRange(e.CurrentSelection.Select(i => (string)i));
    }
}
