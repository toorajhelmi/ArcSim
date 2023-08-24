using Research.ArcSim.Samples.ECommerce;
using Rsearch.ArcSim.Simulator;
using Research.ArcSim.Builder;
using Research.ArcSim.Modeling.Arc;
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Simulation;
using Research.ArcSim.Samples;
using Research.ArcSim.Allocator;
using Research.ArcSim.Handler;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Physical;
using Research.ArcSim.Modeling.Common;
using Research.ArcSim.Modeling.Logincal;

var simulationConfig = new SimulationConfig();
//var ecomm = new EcommerceSystem();
//simulationConfig.SystemDefinition = new SystemDefinition
//{
//    Name = "Tiny System",
//    ModuleCount = 3,
//    AvgfunctionsPerModule = 3,
//    InterModularDependency = ModuleDependency.None,
//    IntraModularDependency = false,
//    ActivityParallelization = Parallelization.InterActivity,   
//};
simulationConfig.SystemDefinition = new SystemDefinition
{
    Name = "Large System",
    ModuleCount = 10,
    AvgfunctionsPerModule = 10,
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

var executionProfile = new ExecutionDemand(
    processingLevel: DemandLevel.Medium,
    memoryLevel: DemandLevel.High,
    bandwithLevel: DemandLevel.High);

simulationConfig.SimulationStrategy = new SimulationStrategy
{
    TotalCost = 10000000,
    MaxResponseTime = 1000,
    RequestDistribution = RequestDistribution.Uniform,
    SimulationDurationSecs = 10 * Units.Minute / 1000,
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

var internet = new BandwidthProfile(12.5 * Units.MB_KB);
var intranet = new BandwidthProfile(100 * Units.MB_KB);

simulationConfig.Bandwidth = new Bandwidth(internet, intranet);

var system = SystemGenerator.Instance.GenerateSystem(simulationConfig.SystemDefinition, false, false, executionProfile);

SystemGenerator.Instance.ShowSystem(system);

foreach (var serverStyle in new[] {
    DeploymentStyle.Microservices,
    DeploymentStyle.Serverless,
    DeploymentStyle.Layered,
    DeploymentStyle.Monolith
})
{
    foreach (var processingMode in new[] {
        ProcessingMode.FireForget,
        //ProcessingMode.Queued,
    })
    {
        simulationConfig.Arch = new Arch
        {
            DeploymentStyle = serverStyle,
            ProcessingMode = processingMode,
        };

        simulationConfig.AllocationStrategy.Stickiness = Mandates.Get<DeploymentStyle, Stickiness>().GetFor(simulationConfig.Arch.DeploymentStyle);

        var impl = Builder.Instance.Build(system, simulationConfig.Arch);
        Builder.Instance.ShowImplementation();

        FireForgetHandler.Create(simulationConfig);
        Allocator.Create(simulationConfig, impl);
        //Allocator.Create(simulationStrategy, costProfile, impl, new Bandwidth(0.001 * Units.MB_KB, 0.9, false));
        Simulator.Create(simulationConfig, system);
        Simulator.Instance.Run(impl);
    }
}