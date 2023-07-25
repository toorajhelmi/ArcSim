﻿using System;
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
			Console.WriteLine(new string('-', 30));
			Console.WriteLine($"Logical Architecture");
			Console.WriteLine($"Server Style: {Enum.GetName<ServerStyle>(implementation.Arch.Style.ServerStyle)}");
            Console.WriteLine($"Client Style: {Enum.GetName<ClientStyle>(implementation.Arch.Style.ClientStyle)}");
            Console.WriteLine($"{implementation.Components.Count} Components");
            Console.WriteLine($"{implementation.Components.Average(c => c.Activities.Count)} Avg Activity per Components");
            Console.WriteLine(new string('-', 30));
			Console.WriteLine();
		}

		private void BuildServer()
		{
			switch (implementation.Arch.Style.ServerStyle)
			{
				case ServerStyle.Microservices: BuildMicroservices();
					break;
                case ServerStyle.Layered: BuildLayered();
                    break;
                //case ServerStyle.Peer2Peer:
                //    break;
                case ServerStyle.Serverless: BuildServerless();
                    break;
				case ServerStyle.Monolith: BuildMonolith();
					break;
            }
        }

        private void BuildMonolith()
        {
			var monolith = new AS.Component { Name = "Monolith" };
			monolith.Activities.AddRange(implementation.System.Modules
					.SelectMany(m => m.Functions)
					.SelectMany(f => f.Activities));

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
				serverless.Activities.Add(activity);
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

