
using System;
using System.Xml.Linq;
using Research.ArcSim.Modeling;
using Research.ArcSim.Modeling.Arc;
using Research.ArcSim.Modeling.Common;
using Research.ArcSim.Modeling.Core;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Physical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Allocator;

public enum ScaleEvent
{
    CpuHigh,
    CpuLow
}

public class Allocator
{
    public class ReportSettings
    {
        public bool ShowSummary { get; set; }
        public bool ShowNodeSummaries { get; set; }
        public bool ShowNodeAllocations { get; set; }
        public bool ShowRequestDetails { get; set; }
    }

    private static Dictionary<string, ComputingNode> onDemandConfigs = new();
    private static Dictionary<string, ComputingNode> upfrontConfigs = new();

    public static CostProfile OnDemandCostProfile { get; set; }
    public static CostProfile UpfrontCostProfile { get; set; }

    public List<Allocation> Allocations { get; set; } = new();
    public double CurrentCost { get; set; }
    public static Allocator Instance { get; private set; }

    private SimulationConfig simulationConfig;
    private SimulationStrategy simulationStrategy;
    private AllocationStrategy allocationStrategy;
    private CostProfile costProfile;
    private LogicalImplementation implementation;
    private Bandwidth bandwidth;
    private Dictionary<Component, List<ComputingNode>> scaleGroup = new();
    private Dictionary<Component, List<(ScaleEvent ScaleEvent, int Time)>> scaleEvents = new();
    private Dictionary<Component, int> loadBalancingIndex { get; set; } = new();

    static Allocator()
    {
        //Based on this (Azure Premuim Plan for Azure Functions): https://azure.microsoft.com/en-us/pricing/calculator/?service=functions

        onDemandConfigs.Add("Light", new ComputingNode(1, 3.5 * Units.GB_MB));
        onDemandConfigs.Add("Standard", new ComputingNode(2, 7 * Units.GB_MB));
        onDemandConfigs.Add("Strong", new ComputingNode(4, 14 * Units.GB_MB));

        upfrontConfigs.Add("1", new ComputingNode(1, 8 * Units.GB_MB));
        upfrontConfigs.Add("2", new ComputingNode(2, 16 * Units.GB_MB));
        upfrontConfigs.Add("4", new ComputingNode(4, 32 * Units.GB_MB));
        upfrontConfigs.Add("8", new ComputingNode(8, 64 * Units.GB_MB));
        upfrontConfigs.Add("16", new ComputingNode(16, 128 * Units.GB_MB));
        upfrontConfigs.Add("32", new ComputingNode(32, 265 * Units.GB_MB));
        upfrontConfigs.Add("64", new ComputingNode(64, 512 * Units.GB_MB));
        upfrontConfigs.Add("128", new ComputingNode(128, 1024 * Units.GB_MB));
        upfrontConfigs.Add("192", new ComputingNode(192, 2048 * Units.GB_MB));

        OnDemandCostProfile = new CostProfile
        {
            //vCPU and memory rates are based on Azure Function Premimum Plan: https://azure.microsoft.com/en-us/pricing/details/functions/
            vCpuPerHour = 0.173,
            MemoryGBPerHour = 0.0086964,
            //Network rate is assuming North America internet acess based on: https://azure.microsoft.com/en-us/pricing/details/bandwidth/
            BandwidthCostPerGBInternet = 0.08,
            BandwidthCostPerGBIntranet = 0.02
        };

        UpfrontCostProfile = new CostProfile
        {
            //Used Mdsv2 series 128 and 193 cores to do the calc (https://azure.microsoft.com/en-us/pricing/details/virtual-machines/windows/#pricing) 
            vCpuPerHour = 0.04359375,
            //Used Mdsv2 series @TB and $TB to do the calc (https://azure.microsoft.com/en-us/pricing/details/virtual-machines/windows/#pricing) 
            MemoryGBPerHour = 0.0123,
            BandwidthCostPerGBInternet = 0.08,
            BandwidthCostPerGBIntranet = 0.02
        };
    }

    public static void Create(SimulationConfig simulationConfig, LogicalImplementation implementation)
    {
        Instance = new Allocator();
        Instance.simulationConfig = simulationConfig;
        Instance.simulationStrategy = simulationConfig.SimulationStrategy;
        Instance.allocationStrategy = simulationConfig.AllocationStrategy;
        Instance.implementation = implementation;
        Instance.bandwidth = simulationConfig.Bandwidth;

        Instance.costProfile = simulationConfig.AllocationStrategy.Stickiness == Stickiness.OnDemand ? OnDemandCostProfile : UpfrontCostProfile;
    }

    public void FreeUp(ComputingNode cn, int time)
    {
        Allocations.First(a => a.ComputingNode == cn).To = time;
    }

    public ComputingNode GetServingNode(Request request)
    {
        ComputingNode node = null;
        var stickiness = allocationStrategy.Stickiness;

        if (!(request.RequestingActivity is ExternalActivity))
        {
            node = request.ServingActivity.Definition.Component.AssignedNode;
        }
        else if (stickiness == Stickiness.OnDemand)
        {
            node = AllocateNode(request.ServingActivity.Definition.Component);
        }
        else if (stickiness == Stickiness.Upfront)
        {
            if (scaleGroup.ContainsKey(request.ServingActivity.Definition.Component))
            {
                ScaleHorizontally(request.ServingActivity.Definition.Component);
                node = LoadBalance(request.ServingActivity.Definition.Component);
            }
            else
                node = AllocateNode(request.ServingActivity.Definition.Component);              
        }

        Allocations.Add(new Allocation
        {
            ComputingNode = node,
            From = Simulation.Instance.Now
        });

        return node;
    }

    public void ShowReport(ReportSettings reportSettings)
    {
        var nodes = Allocations.Select(a => a.ComputingNode).Distinct();
        if (reportSettings.ShowSummary)
        {
            var utilizations = nodes.Select(n => new { Node = n, Util = n.GetUtilization(costProfile) }).ToList();
            foreach (var nodeUtil in utilizations)
            {
                nodeUtil.Node.CalculateCost(nodeUtil.Util, costProfile, simulationConfig);
            }

            Console.WriteLine();
            Console.WriteLine("Allocation Report");
            //Console.WriteLine($"Ave CPU: {Allocations.Average(a => a.ComputingNode.Utilization.CpuCost)}|{Allocations.Average(a => a.ComputingNode.Utilization.ProcessingMSec):0}");
            //Console.WriteLine($"Avg Mem: {Allocations.Average(a => a.ComputingNode.Utilization.MemoryCost)}|{Allocations.Average(a => a.ComputingNode.Utilization.SwapingMSec):0}");
            //Console.WriteLine($"Avg Net: {Allocations.Average(a => a.ComputingNode.Utilization.NetworkCost)}|{Allocations.Average(a => a.ComputingNode.Utilization.TransmissionMSec):0}");
            //Console.WriteLine($"Requests (Internet|Intranet|Internal): {Allocations.Sum(a => a.ComputingNode.Utilization.InternetRequestCount)}|{Allocations.Sum(a => a.ComputingNode.Utilization.IntranetRequestCount)}|{Allocations.Sum(a => a.ComputingNode.Utilization.InternalRequestCount)}");

 
            Console.WriteLine($"Stickiness: {Enum.GetName<Stickiness>(allocationStrategy.Stickiness)} ");

            if (allocationStrategy.Stickiness == Stickiness.OnDemand)
            {
                Console.WriteLine($"Total|Avg Dur (mSec): {utilizations.Sum(nodeUtil => nodeUtil.Util.AggDurationMSec):0} | {utilizations.Average(nodeUtil => nodeUtil.Util.AggDurationMSec):0}");
                Console.WriteLine($"Total|Avg Cost ($): {utilizations.Sum(nodeUtil => nodeUtil.Util.TotalCost):0.000000} | {utilizations.Average(nodeUtil => nodeUtil.Util.TotalCost):0.000000}");
            }
            else
            {
                Console.WriteLine($"Total|Avg Dur (mSec): {utilizations.Sum(nodeUtil => nodeUtil.Util.EndTime - nodeUtil.Util.StartTime):0} | {utilizations.Average(nodeUtil => nodeUtil.Util.AggDurationMSec):0}");
                Console.WriteLine($"Total|Avg Cost ($): {utilizations.Sum(nodeUtil => nodeUtil.Util.TotalCost):0.000000} | {utilizations.Average(nodeUtil => nodeUtil.Util.TotalCost):0.000000}");
            }
        }

        if (reportSettings.ShowNodeSummaries)
        {
            Console.WriteLine();

            foreach (var node in nodes)
            {
                Console.WriteLine($"Node {node.Id}");
                Console.WriteLine($"Total Duration: {node.Utilizations.SelectMany(u => u.Utilization).Sum(u => u.TotalMSec):0}");
                Console.WriteLine($"Total Cost: {node.Utilizations.SelectMany(u => u.Utilization).Sum(n => n.TotalCost):0.000000}");
            }
        }

        if (reportSettings.ShowNodeAllocations)
        {
            Console.WriteLine();

            foreach (var node in nodes)
            {
                Console.WriteLine();
                Console.WriteLine($"Node {node.Id}");

                foreach (var coreUtil in node.Utilizations)
                {
                    Console.WriteLine();
                    Console.WriteLine($"- Core {coreUtil.Core}");
                    foreach (var utilization in coreUtil.Utilization)
                    {
                        Console.Write($"Req {utilization.Request.Id}: {utilization.StartTime}|{utilization.EndTime}|{utilization.AssignedCore}".PadRight(30));
                        if (reportSettings.ShowRequestDetails)
                        {
                            Console.WriteLine();
                            utilization.Request.ServingActivity.ShowActivityTree();
                        }
                        if (coreUtil.Utilization.IndexOf(utilization) % 5 == 4)
                            Console.WriteLine();
                    }
                }
            }
        }
    }

    private ComputingNode AllocateNode(Component component)
    {
        ComputingNode node = null;

        if (allocationStrategy.Stickiness == Stickiness.OnDemand)
            return AllocateOnDemand(component);
        else
            return AllocateUpfront(component);
    }

    private ComputingNode AllocateOnDemand(Component component)
    {
        var totalProcessingRate_vCpu_Secs = component.Activities.Sum(a => (double)a.ExecutionProfile.PP.DemandMilliCpuSec / 1000);
        //Assuming we use a vCPU that allows the task to finish processing in 1s. The highest the vCPU, the faster it takes to process
        //so one could think allocating strong is the most cost effective. But the time it takes for CPU independent tasks such as
        //network tranfer or disk access normally dominate the processing time so it is best to select a vCPU based on demand.

        ComputingNode config = null;
        if (totalProcessingRate_vCpu_Secs <= 1)
            config = onDemandConfigs["Light"];
        else if (totalProcessingRate_vCpu_Secs <= 2)
            config = onDemandConfigs["Standard"];
        else
            config = onDemandConfigs["Strong"];

        var node = new ComputingNode(config.vCpu, config.MemoryMB, bandwidth);
        node.Component = component;
        component.AssignedNode = node;
        return node;
    }

    private ComputingNode AllocateUpfront(Component component)
    {
        var memoryDemandMB = component.Activities.Sum(a => (double)a.ExecutionProfile.MP.DemandMB);

        var config = upfrontConfigs.OrderBy(c => c.Value.MemoryMB)
            .First(c => c.Value.MemoryMB > memoryDemandMB).Value;

        var node = new ComputingNode(config.vCpu, config.MemoryMB, bandwidth);
        node.Component = component;
        component.AssignedNode = node;

        if (!scaleEvents.ContainsKey(component))
        {
            scaleEvents.Add(component, new());
            scaleGroup.Add(component, new());
            loadBalancingIndex.Add(component, 0);
        }
        scaleGroup[component].Add(node);

        return node;
    }

    private void ScaleHorizontally(Component component)
    {
        if (allocationStrategy.HorizontalScalingConfig.HorizonalScaling == HorizonalScaling.CpuControlled)
        {
            var cpuUtilization = scaleGroup[component].Average(n => n.GetCpuUtilizationPercent());

            if (cpuUtilization > allocationStrategy.HorizontalScalingConfig.MaxCpuUtilization &&
                scaleGroup[component].Count < allocationStrategy.HorizontalScalingConfig.MaxInstances)
            {
                if (!scaleEvents[component].Any() || Simulation.Instance.Now - scaleEvents[component].Last().Time > allocationStrategy.HorizontalScalingConfig.CooldownPeriod)
                {
                    scaleEvents[component].Add(new(ScaleEvent.CpuHigh, Simulation.Instance.Now));
                    AllocateUpfront(component);
                }
            }
            else
            {
                if (cpuUtilization < allocationStrategy.HorizontalScalingConfig.MinCpuUtilization &&
                    scaleGroup[component].Count > allocationStrategy.HorizontalScalingConfig.MinCpuUtilization)
                {
                    if (!scaleEvents[component].Any() || Simulation.Instance.Now - scaleEvents[component].Last().Time > allocationStrategy.HorizontalScalingConfig.CooldownPeriod)
                    {
                        scaleGroup[component].Remove(scaleGroup[component].First());
                        scaleEvents[component].Add(new(ScaleEvent.CpuLow, Simulation.Instance.Now));
                    }
                }
            }
        }
        else // if (allocationStrategy.HorizontalScalingConfig.HorizonalScaling == HorizonalScaling.QueueControlled)
        {
            ///TODO
        }
    }

    private ComputingNode LoadBalance(Component component)
    {
        switch (allocationStrategy.HorizontalScalingConfig.LoadBalancingStrategy)
        {
            case LoadBalancingStrategy.RoundRobin:
                var node = scaleGroup[component].ElementAt(loadBalancingIndex[component]);
                loadBalancingIndex[component] = (loadBalancingIndex[component] + 1) % scaleGroup[component].Count;
                return node;
            default:
                return scaleGroup[component].First();
        }
    }
}

