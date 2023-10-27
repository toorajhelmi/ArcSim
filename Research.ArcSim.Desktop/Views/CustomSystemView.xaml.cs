using CommunityToolkit.Maui.Views;
using Research.ArcSim.Desktop.ViewModels;

namespace Research.ArcSim.Desktop.Views;

public partial class CustomSystemView : Popup
{
	public CustomSystemView()
	{
		InitializeComponent();

        BindingContext = SimulationViewModel.Instance;
    }

    private void Apply_Clicked(System.Object sender, System.EventArgs e)
    {
        this.Close();
    }
}
