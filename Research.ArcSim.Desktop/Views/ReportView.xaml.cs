using Research.ArcSim.Desktop.ViewModels;

namespace Research.ArcSim.Desktop.Views;

public partial class ReportView : ContentPage
{
	public ReportView()
	{
		InitializeComponent();

		BindingContext = ReportViewModel.Instance;
	}
}
