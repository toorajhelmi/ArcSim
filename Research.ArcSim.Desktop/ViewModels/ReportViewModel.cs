using System.Collections.ObjectModel;
using Research.ArcSim.Allocators;
using Research.ArcSim.Modeling.Simulation;
using static Research.ArcSim.Allocators.Allocator;

namespace Research.ArcSim.Desktop.ViewModels
{
    public class ReportViewModel
	{
        public class ScenarioResult
        {
            public string ScenarioDesc { get; set; }
            public double RequestCount { get; set; }
            public double TotalCost { get; set; }
            public double AverageTime { get; set; }
        }

        private static ReportViewModel instance;
        public static ReportViewModel Instance => instance;

        public ObservableCollection<ScenarioResult> Results { get; set; } = new();

        static ReportViewModel()
        {
            instance = new ReportViewModel();
        }

        public void AppendResults(List<SimulationResult> simulationResults, List<Request> requests)
        {
            Results.Add(new ScenarioResult
            {
                RequestCount = simulationResults.Count,
                TotalCost = Allocator.Instance.AllocationResults.TotalCost,
                AverageTime = Allocator.Instance.AllocationResults.AvgRequestTime
            });
        }
    }
}

