using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Arc;
using Research.ArcSim.Modeling.Core;
using AS = Research.ArcSim.Modeling;

namespace Research.ArcSim.Builder
{
	public class Builder
	{
        private LogicalImplementation implementation;
        public static Builder Instance { get; }

        static Builder() => Instance = new();

        public LogicalImplementation Build(AS.System system, Arch arch)
		{
			implementation = new LogicalImplementation
			{
				System = system,
				Arch = arch
			};

			BuildServer();

			return implementation;
		}

        public void ShowImplementation()
		{
            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                WriteIndented = true
            };

            Console.WriteLine(new string('-', 30));
			Console.WriteLine($"Logical Architecture");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(implementation.Arch, jsonOptions));
			Console.WriteLine($"Server Style: {Enum.GetName<DeploymentStyle>(implementation.Arch.DeploymentStyle)}");
            Console.WriteLine($"Client Style: {Enum.GetName<ClientStyle>(implementation.Arch.ClientStyle)}");
            Console.WriteLine($"{implementation.Components.Count} Components");
            Console.WriteLine($"{implementation.Components.Average(c => c.Activities.Count)} Avg Activity per Components");
            Console.WriteLine(new string('-', 30));
			Console.WriteLine();
		}

		private void BuildServer()
		{
			switch (implementation.Arch.DeploymentStyle)
			{
				case DeploymentStyle.Microservices: BuildMicroservices();
					break;
                case DeploymentStyle.Layered: BuildLayered();
                    break;
                //case ServerStyle.Peer2Peer:
                //    break;
                case DeploymentStyle.Serverless: BuildServerless();
                    break;
				case DeploymentStyle.Monolith: BuildMonolith();
					break;
            }
        }

        private void BuildMonolith()
        {
			var monolith = new AS.Component { Name = "Monolith" };
			monolith.Activities = implementation.System.Modules
					.SelectMany(m => m.Functions)
					.SelectMany(f => f.Activities).ToList();

            foreach (var activity in monolith.Activities)
			{
				activity.Component = monolith;
			}

			implementation.Components.Add(monolith);
        }

        private void BuildServerless()
        {
            
            foreach (var activity in implementation.System.Modules
                    .SelectMany(m => m.Functions)
                    .SelectMany(f => f.Activities))
            {
                var serverless = new AS.Component { Name = "Serverless" };
				serverless.Activities = new List<ActivityDefinition>{ activity };
                activity.Component = serverless;
                implementation.Components.Add(serverless);
            }
        }

        private void BuildMicroservices()
        {
            var activityGroups = implementation.System.Modules
                    .SelectMany(m => m.Functions)
                    .SelectMany(f => f.Activities)
                    .GroupBy(a => a.Function.Module);

            foreach (var ag in activityGroups)
            {
                implementation.Components.Add(new AS.Component
                {
                    Name = ag.Key.Name,
                    Activities = ag.ToList()
                });
            }

            foreach (var component in implementation.Components)
            {
                foreach (var activity in component.Activities)
                {
                    activity.Component = component;
                }
            }
        }

        private void BuildLayered()
		{
			var activityGroups = implementation.System.Modules
					.SelectMany(m => m.Functions)
					.SelectMany(f => f.Activities)
					.GroupBy(a => a.Layer);

			foreach (var ag in activityGroups)
			{
				implementation.Components.Add(new AS.Component
				{
					Name = Enum.GetName<Layer>(ag.Key),
					Activities = ag.ToList()
				});
			}

			foreach (var component in implementation.Components)
			{
				foreach (var activity in component.Activities)
				{
					activity.Component = component;
				}
			}
		}
	}
}

