
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Core;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Allocator;

public class Allocator
{
    private const int hour_Millisec = 60 * 60 * 1000;
    private const int GB_MB = 1000;
    private const int GB_KB = 1000 * 1000;

    private SimulationStrategy strategy;
    private CostProfile costProfile;
    private List<Allocation> Allocations { get; set; } = new();
    private LogicalImplementation implementation;

    public double CurrentCost { get; set; }
    public static Allocator Instance { get; private set; }

    public static void Create(SimulationStrategy strategy, CostProfile costProfile,
        LogicalImplementation implementation)
    {
        Instance = new Allocator();
        Instance.strategy = strategy;
        Instance.costProfile = costProfile;
        Instance.implementation = implementation;
    }

    public ComputingNode Allocate(Activity request)
    {
        var nodeConfig = GetComponentExecProfile(request.Definition.Component);
        var execTime = request.Definition.ExecutionProfile.PP.DemandMilliCpuSec / nodeConfig.vCpu;

        if (request.Definition.ExecutionProfile.MP.DemandMB > nodeConfig.MemoryMB)
        {
            execTime *= request.Definition.ExecutionProfile.MP.TrashingFactor *
                request.Definition.ExecutionProfile.MP.DemandMB /
                nodeConfig.MemoryMB;
        }
        var execCost = nodeConfig.vCpu * execTime * costProfile.CpuCostvCpuSec / 1000 +
            nodeConfig.MemoryMB * execTime * costProfile.MemoryCostPerGBHour / GB_MB / hour_Millisec +
            request.Definition.ExecutionProfile.BP.DemandKB * costProfile.BandwidthCostPerGB / GB_KB;

        if (CurrentCost + execCost > strategy.TotalCost)
        {
            Simulation.Instance.Terminate("Max Cost Research", request);
            return null;
        }
        else
        {
            CurrentCost += execCost;
            var computingNode = new ComputingNode(nodeConfig, request.Definition.Component);
            Allocations.Add(new Allocation
            {
                ComputingNode = computingNode,
                From = Simulation.Instance.Now
            });

            return computingNode;
        }
    }

    public void FreeUp(Event completionEvent)
    {
        Allocations.First(a => a.ComputingNode == completionEvent.Node).To = Simulation.Instance.Now;
    }

    private NodeConfig GetComponentExecProfile(Component component)
    {
        return new NodeConfig
        {
            vCpu = component.Activities.Sum(a => (double)a.ExecutionProfile.PP.DemandMilliCpuSec / 1000),
            BandwidthKBPerSec = component.Activities.Sum(a => a.ExecutionProfile.BP.DemandKB),
            MemoryMB = component.Activities.Sum(a => a.ExecutionProfile.MP.DemandMB),
        };
    }
}

