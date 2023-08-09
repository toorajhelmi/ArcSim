
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Common;
using Research.ArcSim.Modeling.Core;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Physical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Allocator;

public class Allocator
{
    public static Dictionary<string, ComputingNode> Configs = new();

    private SimulationStrategy strategy;
    private CostProfile costProfile;
    private LogicalImplementation implementation;
    private Bandwidth bandwidth;

    public List<Allocation> Allocations { get; set; } = new();
    public double CurrentCost { get; set; }
    public static Allocator Instance { get; private set; }


    static Allocator()
    {
        //Based on this (Azure Premuim Plan for Azure Functions): https://azure.microsoft.com/en-us/pricing/calculator/?service=functions
        Configs.Add("Light", new ComputingNode
        {
            vCpu = 1,
            MemoryMB = 3.5 * Units.GB_MB, //3.5GB
        });
        Configs.Add("Standard", new ComputingNode
        {
            vCpu = 2,
            MemoryMB = 7 * Units.GB_MB, //3.5GB
        });
        Configs.Add("Strong", new ComputingNode
        {
            vCpu = 4,
            MemoryMB = 14 * Units.GB_MB, //3.5GB
        });
    }

    public static void Create(SimulationStrategy strategy, CostProfile costProfile,
        LogicalImplementation implementation, Bandwidth bandwidth)
    {
        Instance = new Allocator();
        Instance.strategy = strategy;
        Instance.costProfile = costProfile;
        Instance.implementation = implementation;
        Instance.bandwidth = bandwidth;
    }

    public ComputingNode Allocate(Request request)
    {
        var node = AllocateNode(request.ServingActivity.Definition.Component);
        var (response, utilization) = node.CalculateProcessingTimeMillisec(request, true);

        //node.Utilization.CpuCost = node.vCpu * response.ProcessingTime * costProfile.vCpuPerHour / Units.Hour_Millisec;
        //node.Utilization.MemoryCost = node.MemoryMB * response.ProcessingTime * costProfile.MemoryGBPerHour / Units.GB_MB / Units.Hour_Millisec;

        //var netUnitCost = 0.0;

        //if (request.GetScope() == RequestScope.Internet)
        //    netUnitCost = costProfile.BandwidthCostPerGBInternet;
        //if (request.GetScope() == RequestScope.Internet)
        //    netUnitCost = costProfile.BandwidthCostPerGBIntranet;

        //utilization.NetworkCost = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB * netUnitCost / Units.GB_KB;

        //if (request.GetScope() != RequestScope.Local)
        //    utilization.BandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB / Units.MB_KB;

        //if (CurrentCost + utilization.TotalCost > strategy.TotalCost)
        //{
        //    Simulation.Instance.Terminate("Max Cost Reached", request.ServingActivity);
        //    return null;
        //}
        //else
        //{
           // CurrentCost += utilization.TotalCost;

            Allocations.Add(new Allocation
            {
                ComputingNode = node,
                From = Simulation.Instance.Now
            });

            return node;
        //}
    }

    public void FreeUp(ComputingNode cn, int time)
    {
        Allocations.First(a => a.ComputingNode == cn).To = time;
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
            Bandwidth = bandwidth,
            Component = component
        };

        return node;
    }

    public void ShowReport()
    {
        var utilizations = Allocations.Select(a => a.ComputingNode.GetUtilization(costProfile));

        Console.WriteLine();
        Console.WriteLine("Allocation Report");
        //Console.WriteLine($"Ave CPU: {Allocations.Average(a => a.ComputingNode.Utilization.CpuCost)}|{Allocations.Average(a => a.ComputingNode.Utilization.ProcessingMSec):0}");
        //Console.WriteLine($"Avg Mem: {Allocations.Average(a => a.ComputingNode.Utilization.MemoryCost)}|{Allocations.Average(a => a.ComputingNode.Utilization.SwapingMSec):0}");
        //Console.WriteLine($"Avg Net: {Allocations.Average(a => a.ComputingNode.Utilization.NetworkCost)}|{Allocations.Average(a => a.ComputingNode.Utilization.TransmissionMSec):0}");
        //Console.WriteLine($"Requests (Internet|Intranet|Internal): {Allocations.Sum(a => a.ComputingNode.Utilization.InternetRequestCount)}|{Allocations.Sum(a => a.ComputingNode.Utilization.IntranetRequestCount)}|{Allocations.Sum(a => a.ComputingNode.Utilization.InternalRequestCount)}");
        Console.WriteLine($"Total Duration: {utilizations.Sum(a => a.TotalMSec):0}");
        Console.WriteLine($"Total Cost: {utilizations.Sum(a => a.TotalCost):0.000000}");
    }
}

