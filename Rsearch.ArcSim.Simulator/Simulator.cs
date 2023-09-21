using System.Text.Json;
using System.Text.Json.Serialization;
using Research.ArcSim.Allocators;
using Research.ArcSim.Extensions;
using Research.ArcSim.Handler;
using Research.ArcSim.Modeling.Core;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;
using Research.ArcSim.Statistics;
using AS = Research.ArcSim.Modeling;

namespace Rsearch.ArcSim.Simulator
{
    public class Simulator
	{
		private SimulationConfig simulationConfig;
        private SimulationStrategy simulationStrategy;
        private AS.System system;
		private IConsole console;
        private List<Request> originalRequests = new();
        public static Simulator Instance { private set; get; }

		public static void Create(SimulationConfig simulationConfig, AS.System system, IConsole console)
		{
			Instance = new Simulator
			{
				simulationConfig = simulationConfig,
				simulationStrategy = simulationConfig.SimulationStrategy,
				system = system,
				console = console
			};
			Simulation.Create();
			StatisticsCalculator<Activity>.Create();
        }
			
        public void Run(LogicalImplementation implementation)
		{
			GenerateRequests();

			var timeIndex = 0;

			while (true)
			{
				var t = Simulation.Instance.requests.ElementAt(timeIndex).Key;
				//if (timeIndex % 10 == 0)
				ShowProgress(timeIndex);

				Simulation.Instance.Now = t;
				if (Simulation.Instance.requests.ContainsKey(t))
				{
					foreach (var request in Simulation.Instance.requests[t])
					{
						if (request.ServingActivity.StartTime == t)
						{
							FireForgetHandler.Instance.Handle(request);
							if (Simulation.Instance.Terminated)
							{
								console.WriteLine($"Terminated... {Simulation.Instance.TerminationReason.Item1}");
								return;
							}
						}
					}
				}

				timeIndex++;
				if (timeIndex == Simulation.Instance.requests.Count)
					break;
			}

            StatisticsCalculator<Activity>.Instance.Log(() =>
				originalRequests.Select(r => r.ServingActivity).ToList());

			console.WriteLine();
            ShowReport();

			Allocator.Instance.ShowReport(new Allocator.ReportSettings
			{
				ShowSummary = true,
				ShowNodeAllocations = false,
				ShowRequestDetails = false
			});

			var bugs = StatisticsCalculator<Activity>.Instance.Any(s => s.ProcessingTime < 0);
			if (bugs.Any())
			{
				console.WriteLine("BUGS: NAGATIVE PROCESSING TIME:");
				foreach (var activity in bugs)
				{
					activity.ShowActivityTree(0);
				}
			}
        }

		private void ShowProgress(int index)
		{
            char[] spinners = { '|', '/', '-', '\\' };
            //const int animationDelay = 100; 

            int progress = index * 100 / Simulation.Instance.requests.Count;

			console.ShowProgress("\r" + "Running Simulation: {0}%, {1}sec", progress, Simulation.Instance.Now / 1000); //, spinners[Simulation.Instance.Now % spinners.Length]);
            //Thread.Sleep(animationDelay);
        }

		private void ShowReport()
		{
            var jsonOptions = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumConverter() },
                WriteIndented = true
            };

            var completedOriginalRequests = originalRequests.Where(r => r.ServingActivity.Completed).Select(r => r.ServingActivity);
			var completedAllRequests = originalRequests.SelectMany(or => or.ServingActivity.Flatten());

            console.WriteLine();
            console.WriteLine("Simulation Report");
            console.WriteLine(System.Text.Json.JsonSerializer.Serialize(simulationConfig, jsonOptions));
            console.WriteLine($"Simulation completed after {Simulation.Instance.Now / 1000} seconds.");
            console.WriteLine($"Request Count (Orig|All): {completedOriginalRequests.Count()}|{completedAllRequests.Count()}");
            console.WriteLine($"Completed Requests: {StatisticsCalculator<Activity>.Instance.CalcStats(a => a.Completed, Stat.Percentage):0.00}%");
            console.WriteLine($"Expired Requests: {StatisticsCalculator<Activity>.Instance.CalcStats(a => a.Expired, Stat.Percentage):0.00}%");
            console.WriteLine($"Avg Proc Time mSec (Orig|All): {completedOriginalRequests.Average(r => r.ProcessingTime):0}|{completedAllRequests.Average(r => r.ProcessingTime):0}");
        }

        private void GenerateRequests()
		{
			//Only API or Presentation activities can be requested
			var requestableActivities = system.Modules
				.SelectMany(m => m.Functions)
				.SelectMany(f => f.Activities)
                //.Where(a => a.Layer == Layer.API || a.Layer == Layer.Presentation)
                .ToList();


			var activityRandomizor = new Random(1);
		
			switch (simulationStrategy.RequestDistribution)
			{
				case RequestDistribution.Uniform:
					var requestInterval = 1000 / simulationStrategy.AvgReqPerSecond;
					for (int t = 0; t < simulationStrategy.SimulationDurationSecs * 1000; t += requestInterval)
					{
						var request = new Request
						{
							ServingActivity = new Activity(requestableActivities.ElementAt(activityRandomizor.Next(requestableActivities.Count)), t),
							RequestingActivity = new ExternalActivity()
						};

						originalRequests.Add(request);
						Simulation.Instance.ScheduleRequest(t, request);
					}
					break;

				//case RequestDistribution.Bursty:
				//	var requestLoadRandomizer = new RandomGenerator();
				//	var requestCounts = requestLoadRandomizer.GenerateRandomNonNegativeIntegers(
    //                    simulation.SimulationDurationSecs,
    //                    simulation.AvgReqPerSecond,
    //                    simulation.AvgReqPerSecond * 2);

    //                for (int t = 0; t < simulation.SimulationDurationSecs; t += 1)
    //                {
    //                    requests.Add(t, new List<Request>());
    //                    for (int i = 0; i < requestCounts[t]; i++)
    //                    {
    //                        requests[t].Add(new Request
    //                        {
    //                            StartTime = Simulation.Instance.Now,
    //                            Activity = new Activity(
				//					requestableActivities.ElementAt(activityRandomizor.Next(requestableActivities.Count)))
    //                        });
    //                    }
    //                }
    //                break;
			}
		}
	}
}