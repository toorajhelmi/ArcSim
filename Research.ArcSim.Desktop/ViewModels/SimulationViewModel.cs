using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Prism.Mvvm;
using Research.ArcSim.Allocators;
using Research.ArcSim.Builders;
using Research.ArcSim.Extensions;
using Research.ArcSim.Handler;
using Research.ArcSim.Modeling.Arc;
using Research.ArcSim.Modeling.Common;
using Research.ArcSim.Modeling.Core;
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
        public class SystemComponent : BindableBase
        {
            private string horizontalTag;
            private string verticalTag;
            public int Id { get; set; }
            public string Name { get; set; }
            public string HorizontalTag
            {
                get => horizontalTag;
                set
                {
                    if (value != null)
                        SetProperty(ref horizontalTag, value);
                }
            }
            public string VerticalTag
            {
                get => verticalTag;
                set
                {
                    if (value != null)
                        SetProperty(ref verticalTag, value);
                }
            }

            public DemandLevel Cpu { get; set; } = DemandLevel.Medium;
            public DemandLevel Mem { get; set; } = DemandLevel.Medium;
            public DemandLevel BW { get; set; } = DemandLevel.Medium;
        }

        private static SimulationViewModel instance;
        private SimulationConfig simulationConfig;
        private ExecutionDemand executionProfile;
        private List<SimulationResult> results = new();
        private bool useCustomSystemDef;
        private string selectedSystemSize = "Tiny";

        public static SimulationViewModel Instance => instance;
        public string SimulationParameters { get; set; }
        public SystemDefViewModel SystemDefViewModel { get; set; } = new SystemDefViewModel();
        public OutputViewModel OutputViewModel { get; set; } = new OutputViewModel();
        public ResultOutputViewModel ResultOutputViewModel { get; set; } = new ResultOutputViewModel();
        public ObservableCollection<LogicalImplementation> Logicals { get; set; } = new();

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
        public List<DemandLevel> DemandOptions { get; set; } = new();

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

        public bool UseCustomSystemDef
        {
            get => useCustomSystemDef;
            set => SetProperty(ref useCustomSystemDef, value);
        }

        public ObservableCollection<SystemComponent> SystemComponents { get; set; } = new();
        public ObservableCollection<string> HorizontalTagOptions { get; set; } = new();
        public ObservableCollection<string> VerticalTagOptions { get; set; } = new();
        public ObservableCollection<ObservableCollection<bool>> Dependencies { get; set; } = new();
        public ObservableCollection<int> Indexes { get; set; } = new();

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
        public Command AddSystemComponentCommand { get; set; }
        public Command ApplySystemComponentCommand { get; set; }
        public Command LoadEcommerceCommand { get; set; }
        public Command LoadFinancialCommand { get; set; }
        public Command ClearCommand { get; set; }

        static SimulationViewModel()
        {
            instance = new SimulationViewModel();
        }

        private SimulationViewModel()
        {
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
            DemandOptions.AddRange(new[] { DemandLevel.Low, DemandLevel.Medium, DemandLevel.High });

            SystemComponents.Add(new SystemComponent { Id = 1 });
            HorizontalTagOptions.AddRange(new[] { "UI", "API", "DB" });

            RunCommand = new (Run);
            ClearCommnad = new(OutputViewModel.Clear);
            AddSystemComponentCommand = new Command(() => AddCustomComponent(null));
            //ApplySystemComponentCommand = new Command(LoadCustomComponents);
            LoadEcommerceCommand = new Command(LoadEcommerce);
            LoadFinancialCommand = new Command(LoadFinancial);
            ClearCommand = new Command(Clear);
        }

        public async void Run()
        {
            Logicals.Clear();
            SetConfig();
            OutputViewModel.Output = "";
            ReportViewModel.Instance.ClearResults();

            Modeling.System system = null;
            if (useCustomSystemDef)
                system = SystemGenerator.Instance.GenerateSystem(SystemComponents
                    .Where(s => !string.IsNullOrEmpty(s.Name))
                    .Select(sc => new Modeling.Logical.SystemComponent
                {
                    Name = sc.Name,
                    ExecutionDemand = new ExecutionDemand(sc.Cpu, sc.Mem, sc.BW),
                    HorizontalTag = sc.HorizontalTag,
                    VerticalTag = sc.VerticalTag,
                    Id = sc.Id
                }).ToList(), Dependencies, SystemDefViewModel, executionProfile);
            else
                system = SystemGenerator.Instance.GenerateSystem(simulationConfig.SystemDefinition, false, false, SystemDefViewModel, executionProfile);

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

        private void Clear()
        {
            SystemComponents.Clear();
            Dependencies.Clear();
            Indexes.Clear();
        }

        private void LoadFinancial()
        {
            Clear();
            SystemComponents.Add(new SystemComponent
            {
                Id = 1,
                Name = "Ext Sys",
                HorizontalTag = "App",
                VerticalTag = "Ext Sys",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 2,
                Name = "Rep/Int",
                HorizontalTag = "App",
                VerticalTag = "Rep/Int",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 3,
                Name = "Ind Mgmt",
                HorizontalTag = "App",
                VerticalTag = "Ind Mgmt",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 4,
                Name = "Mart App",
                HorizontalTag = "App",
                VerticalTag = "Mart App",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 5,
                Name = "Hist Data",
                HorizontalTag = "App",
                VerticalTag = "Hist Data",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 6,
                Name = "Anal",
                HorizontalTag = "App",
                VerticalTag = "Anal",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 7,
                Name = "Common Data",
                HorizontalTag = "Servies",
                VerticalTag = "Common Data",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 8,
                Name = "RT Data",
                HorizontalTag = "Servies",
                VerticalTag = "RT Data",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 9,
                Name = "Mart Data",
                HorizontalTag = "Servies",
                VerticalTag = "Mart Data",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 10,
                Name = "LDA",
                HorizontalTag = "Servies",
                VerticalTag = "LDA",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 11,
                Name = "SDA",
                HorizontalTag = "Exchange",
                VerticalTag = "SDA",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 12,
                Name = "SDM",
                HorizontalTag = "Exchange",
                VerticalTag = "SDA",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 13,
                Name = "UDAT",
                HorizontalTag = "Exchange",
                VerticalTag = "USDA",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 14,
                Name = "UIDX",
                HorizontalTag = "Exchange",
                VerticalTag = "USDA",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 15,
                Name = "USDA",
                HorizontalTag = "Exchange",
                VerticalTag = "USDA",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 16,
                Name = "Offline",
                HorizontalTag = "Exchange",
                VerticalTag = "HDAS",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 17,
                Name = "Nearline",
                HorizontalTag = "Exchange",
                VerticalTag = "HDAS",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 18,
                Name = "HDAS",
                HorizontalTag = "Exchange",
                VerticalTag = "HDAS",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 19,
                Name = "Batch Col",
                HorizontalTag = "Access",
                VerticalTag = "Batch Col",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 20,
                Name = "RT Col",
                HorizontalTag = "Access",
                VerticalTag = "RT Col",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 21,
                Name = "Unst Col",
                HorizontalTag = "Access",
                VerticalTag = "Unst Col",
            });
            SystemComponents.Add(new SystemComponent
            {
                Id = 22,
                Name = "Ext Col",
                HorizontalTag = "Access",
                VerticalTag = "Ext Col",
            });

            for (int i = 0; i < SystemComponents.Count; i++)
            {
                Dependencies.Add(new ObservableCollection<bool>());
                Indexes.Add(i);
            }

            var f = false;
            var t = true;
            ///////////////////////////////// 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,11,12,13,14,15,16,17,18,19,20,21,22 });
            Dependencies[0].AddRange(new[]  { f, f, f, f, f, f, t, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[1].AddRange(new[]  { f, f, f, f, f, f, f, t, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[2].AddRange(new[]  { f, f, f, f, f, f, f, f, t, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[3].AddRange(new[]  { f, f, f, f, f, f, f, f, t, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[4].AddRange(new[]  { f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, t, f, f, f, f });
            Dependencies[5].AddRange(new[]  { f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[6].AddRange(new[]  { f, f, f, f, f, f, f, f, f, t, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[7].AddRange(new[]  { t, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[8].AddRange(new[]  { f, t, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[9].AddRange(new[]  { f, f, f, f, f, t, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[10].AddRange(new[] { f, f, f, f, f, f, t, f, t, t, f, f, f, f, f, f, f, t, f, f, f, f });
            Dependencies[11].AddRange(new[] { f, f, t, t, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[12].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[13].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[14].AddRange(new[] { f, f, f, f, f, f, f, f, t, t, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[15].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[16].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f, f, f, f, f, t, f, f, f, f, f, f });
            Dependencies[17].AddRange(new[] { f, f, f, f, f, f, f, f, t, t, f, f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[18].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f, t, f, f, f, f, f, f, f, f, f, f });
            Dependencies[19].AddRange(new[] { f, f, f, f, f, f, f, t, f, f, f, t, f, f, f, f, f, f, f, f, f, f });
            Dependencies[20].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f, f, t, t, f, f, f, f, f, f, f, f });
            Dependencies[21].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f, t, f, f, f, t, f, f, f, f, f, f });
        }

        private void LoadEcommerce()
        {
            Clear();
            SystemComponents.Add(new SystemComponent
            {
                Id = 1,
                Name = "ADM",
                HorizontalTag = "UI",
                VerticalTag = "ADM",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 2,
                Name = "CUS",
                HorizontalTag = "API",
                VerticalTag = "CUS",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 3,
                Name = "CAT",
                HorizontalTag = "API",
                VerticalTag = "CAT",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 4,
                Name = "QUE",
                HorizontalTag = "API",
                VerticalTag = "QUE",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 5,
                Name = "CHE",
                HorizontalTag = "API",
                VerticalTag = "CHE",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 6,
                Name = "ORD",
                HorizontalTag = "API",
                VerticalTag = "ORD",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 7,
                Name = "CLD",
                HorizontalTag = "Cloud",
                VerticalTag = "CLD",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 8,
                Name = "DB1",
                HorizontalTag = "DB",
                VerticalTag = "CUS",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 9,
                Name = "DB2",
                HorizontalTag = "DB",
                VerticalTag = "CHE",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 10,
                Name = "DB3",
                HorizontalTag = "DB",
                VerticalTag = "ORD",
            });

            SystemComponents.Add(new SystemComponent
            {
                Id = 11,
                Name = "AGG",
                HorizontalTag = "DB",
                VerticalTag = "CLD",
            });

            //foreach (var component in SystemComponents)
            //{
            //    AddCustomComponent(component);
            //}

            for (int i = 0; i < SystemComponents.Count; i++)
            {
                Dependencies.Add(new ObservableCollection<bool>());
                Indexes.Add(i);
            }

            var f = false;
            var t = true;
            Dependencies[0].AddRange(new[] { f, t, t, t, t, t, f, f, f, f, f });
            Dependencies[1].AddRange(new[] { f, f, t, f, f, f, f, t, f, f, f });
            Dependencies[2].AddRange(new[] { f, f, f, f, f, f, t, f, f, f, f });
            Dependencies[3].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, t });
            Dependencies[4].AddRange(new[] { f, f, t, f, f, f, f, f, t, f, f });
            Dependencies[5].AddRange(new[] { f, t, f, f, f, f, f, f, f, t, f });
            Dependencies[6].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, t });
            Dependencies[7].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[8].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[9].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f });
            Dependencies[10].AddRange(new[] { f, f, f, f, f, f, f, f, f, f, f });
        }

        private void AddCustomComponent(SystemComponent component = null)
        {
            if (component == null)
                component = new SystemComponent { Id = 1 };

            if (SystemComponents.Any())
            {
                if (!VerticalTagOptions.Contains(SystemComponents.Last().VerticalTag))
                    VerticalTagOptions.Add(SystemComponents.Last().VerticalTag);

                if (!HorizontalTagOptions.Contains(SystemComponents.Last().HorizontalTag))
                    HorizontalTagOptions.Add(SystemComponents.Last().HorizontalTag);


                if (SystemComponents.Count > 0)
                {
                    component.Id = SystemComponents.Last().Id + 1;
                    component.HorizontalTag = SystemComponents.Last().HorizontalTag;
                    component.VerticalTag = SystemComponents.Last().VerticalTag;
                }
            }

            //Add one cell to each existing row
            for (int r = 0; r < SystemComponents.Count - 1; r++)
            {
                Dependencies.ElementAt(r).Add(false);
            }

            //Initialize the new row
            Dependencies.Add(new ObservableCollection<bool>());
            for (int c = 0; c < SystemComponents.Count; c++)
            {
                Dependencies.Last().Add(false);
            }

            SystemComponents.Add(component);
            Indexes.Add(SystemComponents.Count - 1);
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

            Logicals.Add(impl);

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

