﻿using Research.ArcSim.Modeling.Common;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Modeling.Physical
{
    public class ComputingNode : Node
	{
        public int vCpu { get; set; }
        public double MemoryMB { get; set; }
        public Bandwidth Bandwidth { get; set; }
        //Key is the CPU core index
        public List<(int Core, List<Utilization> Utilization)> Utilizations { get; set; } = new();

        public ComputingNode(int vCpu, double memoryMB, Bandwidth bandwidth = null)
        {
            this.vCpu = vCpu;
            MemoryMB = memoryMB;
            Bandwidth = bandwidth;

            for (int i = 0; i < vCpu; i++)
            {
                Utilizations.Add((i, new List<Utilization>()));
            }
        }

        public void AssignComponent(Component component)
        {
            Component = component;
        }

        public Component Component { get; set; }

        public Utilization StartProcessing(Request request, Parallelization parallelization)
        {
            var minStartTime = int.Max(request.RequestedStartTime, Simulation.Simulation.Instance.Now);
            var utilization = new Utilization(request);

            var notUtilizedCores = Utilizations.Where(u => !u.Utilization.Any());
            var utilizedFreeCores = Utilizations.Where(u => u.Utilization.Any() && u.Utilization.LastOrDefault().EndTime != 0);

            var firstAvailableCore = notUtilizedCores.Any() ? notUtilizedCores.First()
                : utilizedFreeCores.MinBy(u => u.Utilization.Last().EndTime);

            utilization.AssignedCore = firstAvailableCore.Core;
            request.ServingActivity.AssignedCore = firstAvailableCore.Core;
            utilization.StartTime = !firstAvailableCore.Utilization.Any() ? minStartTime : int.Max(minStartTime, firstAvailableCore.Utilization.Last().EndTime);
            firstAvailableCore.Utilization.Add(utilization);
            return utilization;
        }

        public Response CompleteProcessing(Request request, Utilization utilization)
        {
            var swapingMSec = 0.0;

            if (Component.RequiredMemoryMB > MemoryMB)
            {
                //In this case there is a chance that we need to load the required pages from disk. To caculate the time is takes
                //to load the page, we consider the chance of missing the page in memory and multiply that by the time is takes to
                //load from disk. 
                var swapProbability = 1 - MemoryMB / Component.RequiredMemoryMB;
                //We assume a throughput of 1200MB/S which is Premium SSD v2 in Azure
                //https://learn.microsoft.com/en-us/azure/virtual-machines/disks-types

                ///TODO: Mem req does not necessartily means code that has to be brought to mem
                var diskThroughputMBperSec = 1200;
                swapingMSec = 1000 * swapProbability * request.ServingActivity.Definition.ExecutionProfile.MP.DemandMB / diskThroughputMBperSec;
            }

            var processingTime = request.ServingActivity.Definition.ExecutionProfile.PP.DemandMilliCpuSec + swapingMSec;
            var trasmissionMSec = 0.0;

            var bandwidth = Bandwidth.GetKBPerSec(request);
            if (bandwidth == 0)
            {
                trasmissionMSec = double.MaxValue;
                processingTime += double.MaxValue;
            }
            else if (bandwidth != double.MaxValue)
            {
                trasmissionMSec = 1000 * request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB / bandwidth;
                processingTime += trasmissionMSec;
            }

            var minStartTime = int.Max(request.RequestedStartTime, Simulation.Simulation.Instance.Now);
            var lastUtilization = Utilizations.First(u => u.Core == utilization.AssignedCore)
                .Utilization
                .LastOrDefault();
            
            utilization.StartTime = lastUtilization == default ? minStartTime : int.Max(minStartTime, lastUtilization.EndTime);
            utilization.SwapingMSec = (int)swapingMSec;
            utilization.ProcessingMSec = (int)(request.ServingActivity.Definition.ExecutionProfile.PP.DemandMilliCpuSec / vCpu);
            utilization.TransmissionMSec = (int)trasmissionMSec;
            if (request.GetScope() == RequestScope.Internet)
                utilization.InternetBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB / Units.MB;
            else if (request.GetScope() == RequestScope.Internet)
                utilization.IntranetBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB / Units.MB;
            else
                utilization.LocalBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB / Units.MB;

            return new Response(true);
        }

        public AggregatedUtilizaion GetUtilization()
        {
            var combinedUtilizations = CombineUtilizations(Utilizations.SelectMany(u => u.Utilization));
            var totalUtilization = new AggregatedUtilizaion
            {
                StartTime = combinedUtilizations.First().StartTime,
                EndTime = combinedUtilizations.Last().EndTime,
                AggDurationMSec = combinedUtilizations.Sum(cu => cu.TotalMSec),
                InternetBandwidthMB = combinedUtilizations.Sum(cu => cu.InternetBandwidthMB) / Units.MB,
                IntranetBandwidthMB = combinedUtilizations.Sum(cu => cu.IntranetBandwidthMB) / Units.MB,
                LocalBandwidthMB = combinedUtilizations.Sum(cu => cu.LocalBandwidthMB) / Units.MB,
            };

            return totalUtilization;
        }

        public void CalculateTotalCost(AggregatedUtilizaion utilization, CostProfile costProfile, SimulationConfig simulationConfig)
        {
            var duration = simulationConfig.AllocationStrategy.Stickiness == Stickiness.OnDemand ? utilization.AggDurationMSec : utilization.EndTime - utilization.StartTime;
                
            utilization.CpuCost = vCpu * duration * costProfile.vCpuPerHour / Units.Hour;
            utilization.MemoryCost = MemoryMB * duration * costProfile.MemoryGBPerHour / Units.GB_MB / Units.Hour;
            utilization.NetworkCost = utilization.InternetBandwidthMB * costProfile.BandwidthCostPerGBInternet / Units.GB +
                utilization.IntranetBandwidthMB * costProfile.BandwidthCostPerGBIntranet;
        }

        public double CalculateCost(CostProfile costProfile, int till, int window = Units.Minute)
        {
            var cost = 0.0;

            var since = int.Max(0, till - window);

            var utilizedCores = Utilizations.Where(u => u.Utilization.Any());

            if (!utilizedCores.Any())
                return cost;

            var utilizedTime = 0;

            foreach (var coreUtil in utilizedCores)
            {
                var endedWithinWindow = coreUtil.Utilization.Where(u => (u.EndTime == 0 || u.EndTime > since) && u.StartTime < till);
                utilizedTime += endedWithinWindow.Sum(u => (u.EndTime == 0 ? till : int.Min(u.EndTime, till)) - int.Max(since, u.StartTime));

                if (endedWithinWindow.Any())
                {
                    cost += endedWithinWindow.Sum(u => u.InternetBandwidthMB * costProfile.BandwidthCostPerGBInternet / Units.GB + u.IntranetBandwidthMB * costProfile.BandwidthCostPerGBIntranet);
                }
            }

            cost = vCpu * utilizedTime * costProfile.vCpuPerHour / Units.Hour;
            cost = MemoryMB * utilizedTime * costProfile.MemoryGBPerHour / Units.GB_MB / Units.Hour;

            return cost;
        }

        public int GetCpuUtilizationPercent(int till, int window = Units.Minute)
        {
            var since = int.Max(0, till - window);

            var utilizedCores = Utilizations.Where(u => u.Utilization.Any());

            if (!utilizedCores.Any())
                return 0;

            var utilizedTime = 0;

            foreach (var coreUtil in utilizedCores)
            {
                var endedWithinWindow = coreUtil.Utilization.Where(u => (u.EndTime == 0 || u.EndTime > since) && u.StartTime < till);
                utilizedTime += endedWithinWindow.Sum(u => (u.EndTime == 0 ? till : int.Min(u.EndTime, till)) - int.Max(since, u.StartTime));
            }

            return 100 * utilizedTime / (Utilizations.Count * window);
        }

        public List<AggregatedUtilizaion> CombineUtilizations(IEnumerable<Utilization> utilizations)
        {
            if (!Utilizations.Any())
                return new List<AggregatedUtilizaion>();

            var sortedUtilizations = utilizations.OrderBy(u => u.StartTime).ToList();

            var result = new List<AggregatedUtilizaion>();
            var currenUtil = sortedUtilizations[0].Combine();

            for (int i = 1; i < sortedUtilizations.Count; i++)
            {
                AggregatedUtilizaion aggUtil; 
                if (currenUtil.Overlaps(sortedUtilizations[i]))
                {
                    currenUtil = currenUtil.Combine(sortedUtilizations[i]);
                }
                else
                {
                    result.Add(currenUtil.Combine());
                    currenUtil = sortedUtilizations[i];
                }
            }

            result.Add(currenUtil.Combine());
            return result;
        }
    }
}

