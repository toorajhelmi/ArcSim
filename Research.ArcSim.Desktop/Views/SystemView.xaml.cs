using Research.ArcSim.Desktop.ViewModels;

namespace Research.ArcSim.Desktop.Views;

public partial class SystemView : ContentPage
{
	public SystemView()
	{
		InitializeComponent();

		BindingContext = new SimulationViewModel();

    }
}
