using CommunityToolkit.Maui.Views;
using Research.ArcSim.Desktop.ViewModels;

namespace Research.ArcSim.Desktop.Views;

public partial class SimulationView : ContentPage
{
	public SimulationView()
	{
		InitializeComponent();

		BindingContext = SimulationViewModel.Instance;
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

    private async void DefineCustom_Clicked(object sender, EventArgs e)
    {
        var popup = new CustomSystemView();
        await Application.Current.MainPage.ShowPopupAsync(popup);
    }
}
