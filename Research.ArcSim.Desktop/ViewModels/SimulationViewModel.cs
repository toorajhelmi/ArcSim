using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using Prism.Mvvm;
using Research.ArcSim.Allocators;
using Research.ArcSim.Builders;
using Research.ArcSim.Extensions;
using Research.ArcSim.Handler;
using Research.ArcSim.Modeling.Arc;
using Research.ArcSim.Modeling.Common;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Logincal;
using Research.ArcSim.Modeling.Physical;
using Research.ArcSim.Modeling.Simulation;
using Research.ArcSim.Samples;
using Rsearch.ArcSim.Simulator;
using Location = Research.ArcSim.Modeling.Simulation.Location;

namespace Research.ArcSim.Desktop.ViewModels
{
    public class SimulationViewModel: BindableBase
    {
        private SimulationConfig simulationConfig;
        private ExecutionDemand executionProfile;
        private List<SimulationResult> results = new();
        public string SimulationParameters { get; set; }
        public SystemDefViewModel SystemDefViewModel { get; set; } = new SystemDefViewModel();
        public OutputViewModel OutputViewModel { get; set; } = new OutputViewModel();
        public Command RunCommand { get; set; }
        public List<string> DeploymentOptions { get; set; } = new();
        public List<string> SelectedDeploymentOptions { get; set; } = new();
        public List<string> ProcessingOptions { get; set; } = new();
        public List<string> SelectedProcessingOptions { get; set; } = new();
        
        public SimulationViewModel()
        {
            RunCommand = new Command(Run);
            InitializeConfig();

            DeploymentOptions.AddRange(Enum.GetNames<DeploymentStyle>());
            ProcessingOptions.AddRange(Enum.GetNames<ProcessingMode>());
        }

        public async void Run()
        {
            if (!SelectedDeploymentOptions.Any())
            {
                await App.Current.MainPage.DisplayAlert("Alert", "Please select one or more deployment styles.", "OK");
                return;
            }

            if (!SelectedProcessingOptions.Any())
            {
                await App.Current.MainPage.DisplayAlert("Alert", "Please select one or more processing modes.", "OK");
                return;
            }

            try
            {
                simulationConfig = JsonSerializer.Deserialize<SimulationConfig>(SimulationParameters);
            }
            catch (Exception e)
            {
                await App.Current.MainPage.DisplayAlert("Alert", "Invalid simulation config.\r\n" + e.Message, "OK");
                return;
            }

            OutputViewModel.Output = "";

            var system = SystemGenerator.Instance.GenerateSystem(simulationConfig.SystemDefinition, false, false, SystemDefViewModel, executionProfile);

            SystemGenerator.Instance.ShowSystem(system);

            foreach (var serverStyle in SelectedDeploymentOptions.Select(Enum.Parse<DeploymentStyle>))
            {
                foreach (var processingMode in SelectedProcessingOptions.Select(Enum.Parse<ProcessingMode>))
                {
                    RunForConfig(simulationConfig, system, serverStyle, processingMode);
                }
            }
        }

        private void InitializeConfig()
        {
            simulationConfig = new SimulationConfig();

            simulationConfig.SystemDefinition = new SystemDefinition
            {
                Name = "Tiny System",
                ModuleCount = 3,
                AvgfunctionsPerModule = 3,
                InterModularDependency = ModuleDependency.None,
                IntraModularDependency = false,
                ActivityParallelization = Parallelization.InterActivity,
            };
            //simulationConfig.SystemDefinition = new SystemDefinition
            //{
            //    Name = "Large System",
            //    ModuleCount = 10,
            //    AvgfunctionsPerModule = 10,
            //    InterModularDependency = ModuleDependency.None,
            //    IntraModularDependency = false,
            //    ActivityParallelization = Parallelization.InterActivity,
            //};

            simulationConfig.ComputingNodeConfig = new ComputingNodeConfig
            {
                Nic = Nic.CpuDependent,
                Sku = Sku.Range,
                Location = Location.Cloud
            };

            executionProfile = new ExecutionDemand(
                processingLevel: DemandLevel.Medium,
                memoryLevel: DemandLevel.High,
                bandwithLevel: DemandLevel.High);

            simulationConfig.SimulationStrategy = new SimulationStrategy
            {
                TotalCost = 10000000,
                MaxResponseTime = 1000,
                RequestDistribution = RequestDistribution.Uniform,
                SimulationDurationSecs = 1 * Units.Minute / 1000,
                AvgReqPerSecond = 6
            };

            simulationConfig.HandlingStrategy = new HandlingStrategy
            {
                SkipExpiredRequests = false
            };

            simulationConfig.AllocationStrategy = new AllocationStrategy
            {
                HorizontalScalingConfig = new HorizontalScalingConfig
                {
                    HorizonalScaling = HorizonalScaling.CpuControlled,
                    LoadBalancingStrategy = LoadBalancingStrategy.RoundRobin,
                    MinCpuUtilization = 30,
                    MaxCpuUtilization = 70,
                    MinInstances = 1,
                    MaxInstances = 10,
                    DefaultInstance = 1,
                    CooldownPeriod = 5 * Units.Minute
                }
            };

            Mandates.Add(new Mandate<DeploymentStyle, Stickiness>(
                new List<(DeploymentStyle, Stickiness)> { (DeploymentStyle.Serverless, Stickiness.OnDemand) },
                Stickiness.Upfront));

            var internet = new BandwidthProfile(12.5 * Units.MB);
            var intranet = new BandwidthProfile(100 * Units.MB);

            simulationConfig.Bandwidth = new Bandwidth(internet, intranet);

            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                WriteIndented = true
            };

            SimulationParameters = JsonSerializer.Serialize(simulationConfig, jsonOptions);
        }

        private void RunForConfig(SimulationConfig simulationConfig, Modeling.System system, DeploymentStyle serverStyle, ProcessingMode processingMode)
        {
            results.Clear();

            simulationConfig.Arch = new Arch
            {
                DeploymentStyle = serverStyle,
                ProcessingMode = processingMode,
            };

            simulationConfig.AllocationStrategy.Stickiness = Mandates.Get<DeploymentStyle, Stickiness>().GetFor(simulationConfig.Arch.DeploymentStyle);

            var impl = Builder.Instance.Build(system, simulationConfig.Arch, SystemDefViewModel);
            Builder.Instance.ShowImplementation();

            FireForgetHandler.Create(simulationConfig);
            Allocator.Create(simulationConfig, impl);
            Simulator.Create(simulationConfig, system, OutputViewModel);
            Simulator.Instance.Run(impl);
            StoreResults(simulationConfig);

            var requests = Simulation.Instance.requests.Values.SelectMany(r => r).Select(r => new Request
            {
                Id = r.Id.ToString(),
                Start = r.ServingActivity.StartTime,
                Notes = $"DEPS: [{string.Join(',', r.ServingActivity.Definition.Dependencies.Select(d => d.Id))}] EP: [{r.ServingActivity.Definition.ExecutionProfile}]"
            }).ToList();
            ResultsViewModel.Instance.LoadResults(results, requests);
        }

        private void StoreResults(SimulationConfig simulationConfig)
        {
            var simulationResult = new SimulationResult();

            foreach (var node in Allocator.Instance.Allocations.GroupBy(a => a.ComputingNode))
            {
                simulationResult.Conig = simulationConfig;

                //var nodeResults = new NodeResult();
                foreach (var coreUtilization in node.Key.Utilizations)
                {
                    //var coreCombinedUtil = node.Key.CombineUtilizations(coreUtilization.Utilization);
                    foreach (var utillization in coreUtilization.Utilization)
                    {
                        var coreUtil = new CoreUtil
                        {
                            NodeId = node.Key.Id,
                            CoreIndex = coreUtilization.Core,
                            Start = utillization.StartTime,
                            End = utillization.EndTime,
                            RequestId = utillization.Request.Id.ToString()
                        };

                        simulationResult.CoreUtils.Add(coreUtil);
                    }
                }

                var nodeResult = new NodeResult
                {
                    NodeId = node.Key.Id,
                    CoreCount = node.Key.Utilizations.Count,
                    Start = simulationResult.CoreUtils.Min(cu => cu.Start),
                    End = simulationResult.CoreUtils.Max(cu => cu.End),
                };

                for (int time = nodeResult.Start + Units.Sec; time < nodeResult.End; time += Units.Sec)
                {
                    nodeResult.CpuUtilization.Add((time - Units.Sec, node.Key.GetCpuUtilizationPercent(time, Units.Sec)));
                    nodeResult.Cost.Add((time - Units.Sec, 100 * node.Key.CalculateCost(simulationConfig.AllocationStrategy.Stickiness == Stickiness.OnDemand ?
                        Allocator.OnDemandCostProfile : Allocator.UpfrontCostProfile, time, Units.Sec)));
                }

                simulationResult.NodeResults.Add(nodeResult);
            }

            results.Add(simulationResult);
        }
    }
}

