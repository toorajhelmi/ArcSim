// See https://aka.ms/new-console-template for more information
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


//var ecomm = new EcommerceSystem();
var systemDef = new SystemDefinition
{
    Name = "Tiny System",
    ModuleCount = 3,
    AvgfunctionsPerModule = 3,
    ModuleDependency = ModuleDependency.None
};

var system = SystemGenerator.Instance.GenerateSystem(systemDef, false, false);
SystemGenerator.Instance.ShowSystem(system);

var costProfile = new CostProfile
{
    CpuCostvCpuSec = 0.001,
    MemoryCostPerGBHour = 0.01,
    BandwidthCostPerGB = 0.1
};

var simulationStrategy = new SimulationStrategy
{
    TotalCost = 1000,
    MaxResponseTime = 1000,
    RequestDistribution = RequestDistribution.Uniform,
    SimulationDurationSecs = 60,
    AvgReqPerSecond = 1
};

var handlingStratey = new HandlingStrategy
{
    SkipExpiredRequests = false
};

for (var serverStyle = 0; serverStyle < Enum.GetValues<ServerStyle>().Count(); serverStyle++)
{ 
    var arch = new Arch
    {
        Style = new Style
        {
            ServerStyle = (ServerStyle)serverStyle
        }
    };

    var impl = Builder.Instance.Build(system, arch);
    Builder.Instance.ShowImplementation();

    Handler.Create(simulationStrategy, handlingStratey);
    Allocator.Create(simulationStrategy, costProfile, impl);
    Simulator.Create(simulationStrategy, system);
    Simulator.Instance.Run(impl);
}
