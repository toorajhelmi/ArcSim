
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Core;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Allocator;

public class Allocator
{
    private const int hour_Millisec = 60 * 60 * 1000;
    private const int GB_MB = 1024;
    private const int MB_KB = 1024;
    private const int GB_KB = GB_MB * MB_KB;
    public static Dictionary<string, ComputingNode> Configs = new();

    private SimulationStrategy strategy;
    private CostProfile costProfile;
    private List<Allocation> Allocations { get; set; } = new();
    private LogicalImplementation implementation;

    public double CurrentCost { get; set; }
    public static Allocator Instance { get; private set; }

    static Allocator()
    {
        //Based on this (Azure Premuim Plan for Azure Functions): https://azure.microsoft.com/en-us/pricing/calculator/?service=functions
        Configs.Add("Light", new ComputingNode
        {
            vCpu = 1,
            MemoryMB = 3.5 * GB_MB, //3.5GB
            BandwidthKBPerSec = 12.5 * MB_KB //100Mbps
        });
        Configs.Add("Standard", new ComputingNode
        {
            vCpu = 2,
            MemoryMB = 7 * GB_MB, //3.5GB
            BandwidthKBPerSec = 12.5 * MB_KB //100Mbps
        });
        Configs.Add("Strong", new ComputingNode
        {
            vCpu = 4,
            MemoryMB = 14 * GB_MB, //3.5GB
            BandwidthKBPerSec = 12.5 * MB_KB //100Mbps
        });
    }

    public static void Create(SimulationStrategy strategy, CostProfile costProfile,
        LogicalImplementation implementation)
    {
        Instance = new Allocator();
        Instance.strategy = strategy;
        Instance.costProfile = costProfile;
        Instance.implementation = implementation;
    }

    public ComputingNode Allocate(Activity servingActivity, Activity requestingActivity)
    {
        var node = AllocateNode(servingActivity.Definition.Component);
        var execTime = node.EstimateProcessingTimeMillisec(servingActivity, requestingActivity);


        var execCost = node.vCpu * execTime * costProfile.vCpuPerHour / hour_Millisec +
            node.MemoryMB * execTime * costProfile.MemoryGBPerHour / GB_MB / hour_Millisec;

        if (requestingActivity.Definition.Component != null) //External activity such as internet
            execCost += servingActivity.Definition.ExecutionProfile.BP.DemandKB * costProfile.BandwidthCostPerGB / GB_KB;

        if (CurrentCost + execCost > strategy.TotalCost)
        {
            Simulation.Instance.Terminate("Max Cost Research", servingActivity);
            return null;
        }
        else
        {
            CurrentCost += execCost;

            Allocations.Add(new Allocation
            {
                ComputingNode = node,
                From = Simulation.Instance.Now
            });

            return node;
        }
    }

    public void FreeUp(Event completionEvent)
    {
        Allocations.First(a => a.ComputingNode == completionEvent.Node).To = Simulation.Instance.Now;
    }

    private ComputingNode AllocateNode(Component component)
    {
        var totalProcessingRate_vCput_Secs = component.Activities.Sum(a => (double)a.ExecutionProfile.PP.DemandMilliCpuSec / 1000);

        //Assuming we use a vCPU that allows the task to finish processing in 1s. The highest the vCPU, the faster it takes to process
        //so one could think allocating strong is the most cost effective. But the time it takes for CPU independent tasks such as
        //network tranfer or disk access normally dominate the processing time so it is best to select a vCPU based on demand.
        ComputingNode config = null;

        if (totalProcessingRate_vCput_Secs <= 1)
            config = Configs["Light"];
        else if (totalProcessingRate_vCput_Secs <= 2)
            config = Configs["Standard"];
        else
            config = Configs["Strong"];

        var node = new ComputingNode
        {
            vCpu = config.vCpu,
            MemoryMB = config.MemoryMB,
            BandwidthKBPerSec = config.BandwidthKBPerSec,
            Component = component
        };

        return node;
    }
}

