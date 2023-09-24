using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Research.ArcSim.Extensions;
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Arc;
using Research.ArcSim.Modeling.Core;
using AS = Research.ArcSim.Modeling;

namespace Research.ArcSim.Builders
{
	public class Builder
	{
        private LogicalImplementation implementation;
        private IConsole console;

        public static Builder Instance { get; }

        static Builder() => Instance = new();

        public LogicalImplementation Build(AS.System system, Arch arch, IConsole console)
		{
            implementation = new LogicalImplementation
            {
                System = system,
                Arch = arch,
			};

            this.console = console;
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

            console.WriteLine(new string('-', 30));
			console.WriteLine($"Logical Architecture");
            console.WriteLine(JsonSerializer.Serialize(implementation.Arch, jsonOptions));
			console.WriteLine($"Server Style: {Enum.GetName<DeploymentStyle>(implementation.Arch.DeploymentStyle)}");
            console.WriteLine($"Client Style: {Enum.GetName<ClientStyle>(implementation.Arch.ClientStyle)}");
            console.WriteLine($"{implementation.Components.Count} Components");
            console.WriteLine($"{implementation.Components.Average(c => c.Activities.Count)} Avg Activity per Components");
            console.WriteLine(new string('-', 30));
			console.WriteLine();
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

