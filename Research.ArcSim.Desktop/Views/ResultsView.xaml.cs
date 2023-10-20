using Research.ArcSim.Desktop.ViewModels;

namespace Research.ArcSim.Desktop.Views;

public partial class ResultsView : ContentPage
{
	public ResultsView()
	{
		InitializeComponent();

		BindingContext = ResultsViewModel.Instance;
	}
}
