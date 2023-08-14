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


//var ecomm = new EcommerceSystem();
//var systemDef = new SystemDefinition
//{
//    Name = "Tiny System",
//    ModuleCount = 3,
//    AvgfunctionsPerModule = 3,
//    ModuleDependency = ModuleDependency.None
//    ActivityParallelization = Parallelization.InterActivity,
//};
var systemDef = new SystemDefinition
{
    Name = "Large System",
    ModuleCount = 10,
    AvgfunctionsPerModule = 10,
    InterModularDependency = ModuleDependency.None,
    IntraModularDependency = false,
    ActivityParallelization = Parallelization.InterActivity,
};

var computingNodeConfig = new ComputingNodeConfig
{
    Nic = Nic.CpuDependent,
    Sku = Sku.Range,
    Location = Location.Cloud
};

var executionProfile = new ExecutionDemand(
    processingLevel: DemandLevel.Medium,
    memoryLevel: DemandLevel.High,
    bandwithLevel: DemandLevel.High);

var system = SystemGenerator.Instance.GenerateSystem(systemDef, false, false, executionProfile);
SystemGenerator.Instance.ShowSystem(system);

var simulationStrategy = new SimulationStrategy
{
    TotalCost = 10000000,
    MaxResponseTime = 1000,
    RequestDistribution = RequestDistribution.Uniform,
    SimulationDurationSecs = 60,
    AvgReqPerSecond = 1
};

var handlingStratey = new HandlingStrategy
{
    SkipExpiredRequests = false
};

var allocationStrategy = new AllocationStrategy
{
    Stickiness = Stickiness.Upfront
};

foreach (var serverStyle in new[] {
    //DeploymentStyle.Microservices,
    //DeploymentStyle.Serverless,
    //DeploymentStyle.Layered,
    DeploymentStyle.Monolith
})
{
    foreach (var processingMode in new[] {
        ProcessingMode.FireForget,
        //ProcessingMode.Queued,
    })
    {
        var arch = new Arch
        {
            DeploymentStyle = serverStyle,
            ProcessingMode = ProcessingMode.Queued
        };

        var impl = Builder.Instance.Build(system, arch);
        Builder.Instance.ShowImplementation();

        FireForgetHandler.Create(simulationStrategy, handlingStratey, arch, systemDef);
        var internet = new BandwidthProfile(12.5 * Units.MB_KB);
        var intranet = new BandwidthProfile(100 * Units.MB_KB);
        Allocator.Create(simulationStrategy, impl, allocationStrategy, new Bandwidth(internet, intranet));
        //Allocator.Create(simulationStrategy, costProfile, impl, new Bandwidth(0.001 * Units.MB_KB, 0.9, false));
        Simulator.Create(simulationStrategy, system);
        Simulator.Instance.Run(impl);
    }
}