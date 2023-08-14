using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Research.ArcSim.Extensions;
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Logical;
using AS = Research.ArcSim.Modeling;

namespace Research.ArcSim.Samples
{
	public class SystemGenerator
	{
        public static SystemGenerator Instance { get; }
        static SystemGenerator() => Instance = new();
        private ExecutionDemand executionProfile;

        public AS.System GenerateSystem(SystemDefinition defintion, bool randomizeSystem,
            bool randomizeDemand, ExecutionDemand executionProfile = null)
        {
            this.executionProfile = executionProfile;
            var system = GenerateSystem(defintion, randomizeSystem);
            system.SystemDefinition = defintion;

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

            Console.WriteLine(new string('=', 30));
            Console.WriteLine($"Name: {system.SystemDefinition.Name}");
            Console.WriteLine($"{system.Modules.Count} Modules");
            Console.WriteLine($"{system.Modules.Average(m => m.Functions.Count)} Avg Functions per Module");
            Console.WriteLine($"{system.Modules.Average(m => m.Functions.Average(f => f.Activities.Count)):0.00} Avg Activities per Function");
            Console.WriteLine($"{activities.Average(a => a.Dependencies.Count(d => a.Function.Module == d.Function.Module)):0.00} Avg Intra-Modular Dependency");
            Console.WriteLine($"{activities.Average(a => a.Dependencies.Count(d => a.Function.Module != d.Function.Module)):0.00} Avg Inter-Modular Dependency");
            Console.WriteLine($"Execution Profile:");
            Console.WriteLine($"- CPU: {executionProfile.PP.DemandMilliCpuSec}vCpu x MilliSec");
            Console.WriteLine($"- Mem: {executionProfile.MP.DemandMB}MB");
            Console.WriteLine($"- Net: {executionProfile.BP.DemandKB}KB");
            Console.WriteLine(new string('=', 30));
            Console.WriteLine();

            //foreach (var modelule in system.Modules)
            //{
            //    Console.WriteLine(new string('=', 30));
            //    Console.WriteLine($"Module Name: {modelule.Name}, Functions Count : {system.Modules.Count}");
            //    Console.WriteLine(new string('=', 30));

            //    foreach (var function in modelule.Functions)
            //    {
            //        Console.WriteLine($"Module Name: {modelule.Name}");
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
                        Layer = AS.Layer.Presentation,
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
                    foreach (var activity in function.Activities.Where(a => a.Layer == Layer.Presentation))
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
    }
}

