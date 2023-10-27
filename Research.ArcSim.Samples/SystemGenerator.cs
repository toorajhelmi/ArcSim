using System;
using System.Collections.ObjectModel;
using Research.ArcSim.Extensions;
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Logical;
using AS = Research.ArcSim.Modeling;

namespace Research.ArcSim.Samples
{
    public class SystemGenerator
	{
        static SystemGenerator() => Instance = new();
        private ExecutionDemand executionProfile;
        private IConsole console;
        public static SystemGenerator Instance { get; }

        public AS.System GenerateSystem(SystemDefinition definition, bool randomizeSystem,
            bool randomizeDemand, IConsole console, ExecutionDemand executionProfile = null)
        {
            this.executionProfile = executionProfile;
            this.console = console;
            var system = GenerateSystem(definition, randomizeSystem);
            system.SystemDefinition = definition;

            var random = new Random();
            var activities = system.Modules.SelectMany(m => m.Functions).SelectMany(f => f.Activities);

            foreach (var activity in activities)
            {
                if (randomizeDemand)
                {
                    activity.ExecutionProfile.PP.Set((DemandLevel)random.Next(3));
                    activity.ExecutionProfile.BP.Set((DemandLevel)random.Next(3));
                    activity.ExecutionProfile.MP.Set((DemandLevel)random.Next(3));
                }
                else
                {
                    activity.ExecutionProfile = executionProfile;
                }
            }

            return system;
        }

        public void ShowSystem(AS.System system)
        {
            var activities = system.Modules.SelectMany(m => m.Functions.SelectMany(f => f.Activities)).ToList();

            console.WriteLine(new string('=', 30));
            console.WriteLine($"Name: {system.SystemDefinition?.Name}");
            console.WriteLine($"{system.Modules.Count} Modules");
            console.WriteLine($"{system.Modules.Average(m => m.Functions.Count)} Avg Functions per Module");
            console.WriteLine($"{system.Modules.Average(m => m.Functions.Average(f => f.Activities.Count)):0.00} Avg Activities per Function");
            console.WriteLine($"{activities.Average(a => a.Dependencies.Count(d => a.Function.Module == d.Function.Module)):0.00} Avg Intra-Modular Dependency");
            console.WriteLine($"{activities.Average(a => a.Dependencies.Count(d => a.Function.Module != d.Function.Module)):0.00} Avg Inter-Modular Dependency");
            console.WriteLine($"Execution Profile:");
            console.WriteLine($"- CPU: {executionProfile.PP.DemandMilliCpuSec}vCpu x MilliSec");
            console.WriteLine($"- Mem: {executionProfile.MP.DemandMB}MB");
            console.WriteLine($"- Net: {executionProfile.BP.DemandKB}KB");
            console.WriteLine(new string('=', 30));
            console.WriteLine();

            //foreach (var modelule in system.Modules)
            //{
            //    console.WriteLine(new string('=', 30));
            //    console.WriteLine($"Module Name: {modelule.Name}, Functions Count : {system.Modules.Count}");
            //    console.WriteLine(new string('=', 30));

            //    foreach (var function in modelule.Functions)
            //    {
            //        console.WriteLine($"Module Name: {modelule.Name}");
            //    }
            //}

        }

        private AS.System GenerateSystem(SystemDefinition definition, bool randomizeSystem)
        {
            var random = new Random();
            var system = new AS.System();

            var moduleCount = randomizeSystem ? random.Next(definition.ModuleCount) + 1 : definition.ModuleCount;

            for (int moduleIndex = 0; moduleIndex < moduleCount; moduleIndex++)
            {
                var module = new AS.Module { Name = $"Module {moduleIndex}" };
                system.Modules.AddX(module);

                var functionsPerModule = randomizeSystem ? random.Next(definition.AvgfunctionsPerModule) + 1
                    : definition.AvgfunctionsPerModule;
             
                for (int functionIndex = 0; functionIndex < functionsPerModule; functionIndex++)
                {
                    var function = new AS.Function
                    {
                        Name = $"Function {moduleIndex}_{functionIndex}",
                        Module = module
                    };

                    module.Functions.AddX(function);

                    var dbActivity = new AS.ActivityDefinition
                    {
                        Name = $"Activity {moduleIndex}_{functionIndex}_DB",
                        Layer = AS.Layer.DB,
                        Function = function
                    };
                    var apiActivity = new AS.ActivityDefinition
                    {
                        Name = $"Activity {moduleIndex}_{functionIndex}_API",
                        Layer = AS.Layer.API,
                        Dependencies = !definition.IntraModularDependency ? new List<AS.ActivityDefinition>() : new List<AS.ActivityDefinition> { dbActivity },
                        Function = function
                    };
                    var prActivity = new AS.ActivityDefinition
                    {
                        Name = $"Activity {moduleIndex}_{functionIndex}_PR",
                        Layer = AS.Layer.UI,
                        Dependencies = !definition.IntraModularDependency ? new List<AS.ActivityDefinition>() : new List<AS.ActivityDefinition> { apiActivity },
                        Function = function
                    };

                    var activityCount = randomizeSystem ? random.Next(3) + 1 : 3;
                    
                    if (activityCount >= 1)
                        function.Activities.AddX(dbActivity);

                    if (activityCount >= 2)
                        function.Activities.AddX(apiActivity);

                    if (activityCount >= 3)
                        function.Activities.AddX(prActivity);
                }
            }

            //Setting inter modular dependencies
            var dependencyCount = system.Modules.Count * (int)definition.InterModularDependency
                 / Enum.GetValues<ModuleDependency>().Count();

            foreach (var module in system.Modules)
            {
                module.Dependencies = system.Modules.Except(new[] { module })
                    .OrderBy(item => random.Next()).Take(dependencyCount).ToList();

                foreach (var function in module.Functions)
                {
                    //Assuming only presentation level acitivities can depend on other modules
                    foreach (var activity in function.Activities.Where(a => a.Layer == Layer.UI))
                    {
                        foreach (var depModule in module.Dependencies)
                        {
                            //Assuming the dependency activities could only be at the API layer
                            var depActivities = depModule.Functions.SelectMany(s => s.Activities).Where(a => a.Layer == Layer.API);
                            var depActivity = depActivities.ElementAt(random.Next(depActivities.Count()));
                            activity.Dependencies.Add(depActivity);
                        }                  
                    }
                }
            }

            return system;
        }

        public AS.System GenerateSystem(List<SystemComponent> systemComponents,
            ObservableCollection<ObservableCollection<bool>> dependencies, IConsole console, ExecutionDemand executionProfile)
        {
            this.console = console;
            this.executionProfile = executionProfile;
            var system = new AS.System();
            var moduleNames = systemComponents.Select(s => s.VerticalTag).Distinct();
            var layersNames = systemComponents.Select(s => s.HorizontalTag).Distinct();

            foreach (var moduleName in moduleNames)
            {
                var module = new AS.Module { Name = moduleName };
                system.Modules.AddX(module);

                foreach (var activityGroup in systemComponents.Where(s => s.VerticalTag == moduleName).GroupBy(s => s.Name))
                {
                    var function = new Modeling.Function
                    {
                        Name = activityGroup.Key,
                        Module = module
                    };

                    module.Functions.AddX(function);

                    foreach (var activityDef in activityGroup)
                    {
                        var activity = new ActivityDefinition
                        {
                            Name = $"Activity {moduleName}_{function.Name}_{activityDef.HorizontalTag}",
                            Layer = activityDef.HorizontalTag,
                            Dependencies = new List<ActivityDefinition>(),
                            Function = function,
                            Id = activityDef.Id
                        };

                        activity.ExecutionProfile.PP.Set(activityDef.Cpu);
                        activity.ExecutionProfile.BP.Set(activityDef.BW);
                        activity.ExecutionProfile.MP.Set(activityDef.Mem);

                        function.Activities.Add(activity);
                    };
                }
            }

            var activities = system.Modules.SelectMany(m => m.Functions).SelectMany(f => f.Activities);

            foreach (var activity in activities)
            {
                var deps = dependencies.ElementAt(systemComponents.IndexOf(systemComponents.First(s => s.Id == activity.Id)));
                for (int depIndex = 0; depIndex < dependencies.Count; depIndex++)
                {
                    if (deps[depIndex])
                    {
                        activity.Dependencies.Add(activities.First(a => a.Id == depIndex + 1));
                    }
                }
            }

            return system;
        }
    }
}

