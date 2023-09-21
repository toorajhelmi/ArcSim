using Research.ArcSim.Desktop.ViewModels;

namespace Research.ArcSim.Desktop.Views;

public partial class SimulationView : ContentPage
{
	public SimulationView()
	{
		InitializeComponent();

		BindingContext = ResultsViewModel.Instance;
	}
}
