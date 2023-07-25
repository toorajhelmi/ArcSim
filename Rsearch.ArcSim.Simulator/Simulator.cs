using System;
using Research.ArcSim.Allocator;
using Research.ArcSim.Handler;
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Core;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;
using Research.ArcSim.Statistics;
using AS = Research.ArcSim.Modeling;

namespace Rsearch.ArcSim.Simulator
{
	public class Simulator
	{
		public static Simulator Instance { private set; get; }

		public static void Create(SimulationStrategy simulationStrategy, AS.System system)
		{
			Instance = new Simulator
			{
				simulationStrategy = simulationStrategy,
				system = system
			};
		}

		private SimulationStrategy simulationStrategy;
		private AS.System system;
		private List<Activity> originalRequests = new();
			
        public void Run(LogicalImplementation implementation)
		{
			GenerateRequests();

			var timeIndex = 0;

			while (true)
			{
				var t = Simulation.Instance.requests.ElementAt(timeIndex).Key;
				if (t % 1000 == 0)
					ShowProgress();
				Simulation.Instance.Now = t;
				if (Simulation.Instance.requests.ContainsKey(t))
				{
					foreach (var request in Simulation.Instance.requests[t])
					{
						if (request.StartTime == t)
						{
							Handler.Instance.Handle(request, new ExternalActivity(), new List<Activity>());
							if (Simulation.Instance.Terminated)
							{
								Console.WriteLine($"Terminated... {Simulation.Instance.TerminationReason.Item1}");
								break;
							}
						}
					}
				}

				var events = Simulation.Instance.GetEvents(t);
				var completionEvents = events.Where(e => e.EventType == EventType.ActivityCompleted);

                foreach (var completionEvent in completionEvents)
				{
					Allocator.Instance.FreeUp(completionEvent);
				}

				timeIndex++;
				if (timeIndex == Simulation.Instance.requests.Count)
					break;
			}

			StatisticsCalculator<Activity>.Instance.Log(() => originalRequests);

			Console.WriteLine();
            ShowReport();
        }

		private void ShowProgress()
		{
            char[] spinners = { '|', '/', '-', '\\' };
            const int animationDelay = 100; 

            int progress = Simulation.Instance.Now * 100 / Simulation.Instance.requests.Last().Key;

			Console.Write("\r" + "Running Simulation: {0}%", progress); //, spinners[Simulation.Instance.Now % spinners.Length]);
            Thread.Sleep(animationDelay);
        }

		private void ShowConfig()
		{

		}

		private void ShowReport()
		{
			var allRequests = originalRequests;
            var completedRequests = allRequests.Where(r => r.Completed);

            Console.WriteLine();
			Console.WriteLine($"Simulation completed after {Simulation.Instance.Now / 1000} seconds.");
            Console.WriteLine($"Request Count: {StatisticsCalculator<Activity>.Instance.CalcStats(a => true, Stat.Average)}");
            Console.WriteLine($"Completed Requests: {StatisticsCalculator<Activity>.Instance.CalcStats(a => a.Completed, Stat.Percentage):0.00}%");
            Console.WriteLine($"Expired Requests: {StatisticsCalculator<Activity>.Instance.CalcStats(a => a.Expired, Stat.Percentage):0.00}%");
            Console.WriteLine($"Average Processing Time: {completedRequests.Average(r => r.ProcessingTime):0} mSec");
            Console.WriteLine($"Total Cost: ${Allocator.Instance.CurrentCost:0.000}");
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
						var request = new Activity(
                            requestableActivities.ElementAt(activityRandomizor.Next(requestableActivities.Count)), t);
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