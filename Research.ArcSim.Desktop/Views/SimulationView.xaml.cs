using Research.ArcSim.Desktop.ViewModels;

using static Research.ArcSim.Desktop.ViewModels.Request;

namespace Research.ArcSim.Desktop.Views;

public partial class SimulationView : ContentPage
{
	public SimulationView()
	{
		InitializeComponent();

		BindingContext = new SimulationViewModel(null);
	}
}
