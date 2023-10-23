using System.Text.Json;
using System.Text.Json.Serialization;
using Prism.Mvvm;
using Research.ArcSim.Allocators;
using Research.ArcSim.Builders;
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
    public class SimulationViewModel : BindableBase
    {
        private SimulationConfig simulationConfig;
        private ExecutionDemand executionProfile;
        private List<SimulationResult> results = new();
        public string SimulationParameters { get; set; }
        public SystemDefViewModel SystemDefViewModel { get; set; } = new SystemDefViewModel();
        public OutputViewModel OutputViewModel { get; set; } = new OutputViewModel();
        public ResultOutputViewModel ResultOutputViewModel { get; set; } = new ResultOutputViewModel();

        private string selectedSystemSize = "Tiny";

        public List<string> DeploymentOptions { get; set; } = new();
        public List<string> SelectedDeploymentOptions { get; set; } = new();
        public List<string> ProcessingOptions { get; set; } = new();
        public List<string> SelectedProcessingOptions { get; set; } = new();
        public List<string> SystemSizeOptions { get; set; } = new();
        public List<string> DependencyOptions { get; set; } = new();
        public List<string> YesNoOptions { get; set; } = new();
        public List<string> AllocationModeOptions { get; set; } = new();
        public List<string> HorizontalScalingOptions { get; set; } = new();
        public List<string> LoadBalancingStrategyOptions { get; set; } = new();
        public List<string> RequestDistributionOptions { get; set; } = new();
        public List<string> BandwidthPattenOptions { get; set; } = new();
        
        public string SelectedSystemSize
        {
            get => selectedSystemSize;
            set
            {
                SetProperty(ref selectedSystemSize, value);
                switch (selectedSystemSize)
                {
                    case "Tiny":
                        ModuleCount = 3;
                        AvgFunctionPerModule = 3;
                        InterModularDependency = "None";
                        IntraModularDependency = "No";
                        break;
                    case "Large":
                        ModuleCount = 10;
                        AvgFunctionPerModule = 10;
                        InterModularDependency = "High";
                        IntraModularDependency = "No";
                        break;
                }
            }
        }

        public int ModuleCount { get; set; } = 3;
        public int AvgFunctionPerModule { get; set; } = 3;
        public string IntraModularDependency { get; set; } = "No";
        public string InterModularDependency { get; set; } = "None";
        public string AllocationMode { get; set; } = "On Demand";
        public string HorizontalScaling { get; set; } = "Cpu Controlled";
        public int MinCpuUtilization { get; set; } = 30;
        public int MaxCpuUtilization { get; set; } = 70;
        public int MinQueueLength { get; set; } = 3;
        public int MaxQueueLength { get; set; } = 7;
        public int CooldownPeriod { get; set; } = 5 * Units.Minute;
        public int DefaultInstances { get; set; } = 1;
        public int MinInstances { get; set; } = 10;
        public int MaxInstances { get; set; } = 10;
        public string LoadBalancingStrategy { get; set; } = "Round Robin";
        public string SkipExpiredRequests { get; set; } = "No";
        public int TrialCount { get; set; } = 0;
        public int MaxResponseTime { get; set; } = 1000;
        public int TotalCost { get; set; } = 100000;
        public string RequestDistribution { get; set; } = "Uniform";
        public int AvgRequestPerSecond { get; set; } = 6;
        public int SimulationDuration { get; set; } = 60;
        public string InternetBandwidthPatten { get; set; } = "Fixed";
        public int MaxInternetBandwidthKB { get; set; } = 12800;
        public int InternetVarationPercent { get; set; } = 0;
        public string IntranetBandwidthPatten { get; set; } = "Fixed";
        public int MaxIntranetBandwidthKB { get; set; } = 102400;
        public int IntranetVarationPercent { get; set; } = 0;

        public Command RunCommand { get; set; }
        public Command ClearCommnad { get; set; }

        public SimulationViewModel()
        {
            RunCommand = new(Run);
            ClearCommnad = new(OutputViewModel.Clear);
            InitializeConfig();

            DeploymentOptions.AddRange(Enum.GetNames<DeploymentStyle>());
            ProcessingOptions.AddRange(Enum.GetNames<ProcessingMode>());
            SystemSizeOptions.AddRange(new[] { "Tiny", "Large" });
            DependencyOptions.AddRange(new[] { "None", "Low", "Medium", "High", "Extreme" });
            YesNoOptions.AddRange(new[] { "Yes", "No" });
            AllocationModeOptions.AddRange(new[] { "On Demand", "Up Front" });
            HorizontalScalingOptions.AddRange(new[] { "None", "Cpu Controlled", "Queue Controlled" });
            LoadBalancingStrategyOptions.AddRange(new[] { "Round Robin", "Least Utilized", "Least Reponse Time" });
            RequestDistributionOptions.AddRange(new[] { "Uniform", "Bursty" });
            BandwidthPattenOptions.AddRange(new[] { "Fix", "Uniform" });

        }

        public async void Run()
        {
            SetConfig();
            OutputViewModel.Output = "";
            ReportViewModel.Instance.ClearResults();

            var system = SystemGenerator.Instance.GenerateSystem(simulationConfig.SystemDefinition, false, false, SystemDefViewModel, executionProfile);

            SystemDefViewModel.Clear();
            SystemGenerator.Instance.ShowSystem(system);

            foreach (var serverStyle in SelectedDeploymentOptions.Select(Enum.Parse<DeploymentStyle>))
            {
                foreach (var processingMode in SelectedProcessingOptions.Select(Enum.Parse<ProcessingMode>))
                {
                    RunForConfig(simulationConfig, system, serverStyle, processingMode);
                }
            }
        }

        private async void SetConfig()
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

            simulationConfig.SystemDefinition = new SystemDefinition
            {
                ModuleCount = ModuleCount,
                AvgfunctionsPerModule = AvgFunctionPerModule,
                InterModularDependency = Enum.Parse<ModuleDependency>(InterModularDependency),
                IntraModularDependency = IntraModularDependency == "Yes",
                ActivityParallelization = Parallelization.InterActivity,
            };

            simulationConfig.SimulationStrategy = new SimulationStrategy
            {
                TotalCost = TotalCost,
                MaxResponseTime = MaxResponseTime,
                RequestDistribution = Enum.Parse<RequestDistribution>(RequestDistribution),
                SimulationDurationSecs = SimulationDuration,
                AvgReqPerSecond = AvgRequestPerSecond
            };

            simulationConfig.HandlingStrategy = new HandlingStrategy
            {
                SkipExpiredRequests = SkipExpiredRequests == "Yes",
                TrialCount = TrialCount
            };

            simulationConfig.AllocationStrategy = new AllocationStrategy
            {
                HorizontalScalingConfig = new HorizontalScalingConfig
                {
                    HorizonalScaling = Enum.Parse<HorizonalScaling>(HorizontalScaling.Replace(" ", "")),
                    LoadBalancingStrategy = Enum.Parse<LoadBalancingStrategy>(LoadBalancingStrategy.Replace(" ", "")),
                    MinCpuUtilization = MinCpuUtilization,
                    MaxCpuUtilization = MaxCpuUtilization,
                    MinInstances = MinInstances,
                    MaxInstances = MaxInstances,
                    DefaultInstance = DefaultInstances,
                    CooldownPeriod = CooldownPeriod
                }
            };

            Mandates.Add(new Mandate<DeploymentStyle, Stickiness>(
                new List<(DeploymentStyle, Stickiness)> { (DeploymentStyle.Serverless, Stickiness.OnDemand) },
                Stickiness.Upfront));

            var internet = new BandwidthProfile(MaxInternetBandwidthKB);
            var intranet = new BandwidthProfile(MaxIntranetBandwidthKB);

            simulationConfig.Bandwidth = new Bandwidth(internet, intranet);
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
                RequestDistribution = Modeling.Simulation.RequestDistribution.Uniform,
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
                    LoadBalancingStrategy = Modeling.Simulation.LoadBalancingStrategy.RoundRobin,
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
            Allocator.Create(simulationConfig, impl, ResultOutputViewModel);
            Simulator.Create(simulationConfig, system, OutputViewModel);
            Simulator.Instance.Run();

            try
            {
                StoreResults(simulationConfig, serverStyle, processingMode);

                var requests = Simulation.Instance.requests.Values.SelectMany(r => r).Select(r => new Request
                {
                    Id = r.Id.ToString(),
                    Start = r.ServingActivity.StartTime,
                    Notes = $"DEPS: [{string.Join(',', r.ServingActivity.Definition.Dependencies.Select(d => d.Id))}] EP: [{r.ServingActivity.Definition.ExecutionProfile}]"
                }).ToList();

                Allocator.Instance.EvaluateResults();
                ResultsViewModel.Instance.LoadResults(results, requests);
                ReportViewModel.Instance.AppendResults(results, requests);
                Allocator.Instance.ShowReport(new Allocator.ReportSettings
                {
                    ShowSummary = true
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StoreResults(SimulationConfig simulationConfig, DeploymentStyle serverStyle, ProcessingMode processingMode)
        {
            var simulationResult = new SimulationResult
            {
                TotalRequests = Simulator.Instance.Results.TotalRequests,
                CompletedRequests = Simulator.Instance.Results.CompletedRequests,
                Descripton = $"{Enum.GetName(serverStyle)}"
            };

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

