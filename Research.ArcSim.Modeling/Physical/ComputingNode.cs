using System;
using Research.ArcSim.Modeling.Common;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Modeling.Physical
{
    public class ComputingNode : Node
	{
        public double vCpu { get; set; }
        public double MemoryMB { get; set; }
        public Bandwidth Bandwidth { get; set; }
        public List<Utilization> Utilizations { get; set; } = new();

        public void AssignComponent(Component component)
        {
            Component = component;
        }

        public Component Component { get; set; }

        public (Response, Utilization) Process(Request request)
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

                var diskThroughputMBperSec = 1200;
                swapingMSec = 1000 * swapProbability * request.ServingActivity.Definition.ExecutionProfile.MP.DemandMB / diskThroughputMBperSec;
            }

            var processingTime = (int)(request.ServingActivity.Definition.ExecutionProfile.PP.DemandMilliCpuSec / vCpu) + swapingMSec;
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

            Utilization utilization = default(Utilization);

            var minStartTime = int.Max(request.RequestedStartTime, Simulation.Simulation.Instance.Now);
            //if (utilize)
            {
                utilization = new Utilization(request);
                utilization.StartTime = !Utilizations.Any() ? minStartTime : int.Max(minStartTime, Utilizations.Last().EndTime);
                utilization.SwapingMSec = (int)swapingMSec;
                utilization.ProcessingMSec = (int)(request.ServingActivity.Definition.ExecutionProfile.PP.DemandMilliCpuSec / vCpu);
                utilization.TransmissionMSec = (int)trasmissionMSec;
                if (request.GetScope() == RequestScope.Internet)
                    utilization.InternetBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB / Units.MB_KB;
                else if (request.GetScope() == RequestScope.Internet)
                    utilization.IntranetBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB / Units.MB_KB;
                else
                    utilization.LocalBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB / Units.MB_KB;


                Utilizations.Add(utilization);
            }

            return (new Response(true), utilization);
        }

        public AggregatedUtilizaion GetUtilization(CostProfile costProfile)
        {
            var combinedUtilizations = CombineUtilizations();
            var totalUtilization = new AggregatedUtilizaion
            {
                AggDurationMSec = combinedUtilizations.Sum(cu => cu.TotalMSec),
                InternetBandwidthMB = combinedUtilizations.Sum(cu => cu.InternetBandwidthMB) / Units.MB_KB,
                IntranetBandwidthMB = combinedUtilizations.Sum(cu => cu.IntranetBandwidthMB) / Units.MB_KB,
                LocalBandwidthMB = combinedUtilizations.Sum(cu => cu.LocalBandwidthMB) / Units.MB_KB,
            };

            totalUtilization.CpuCost = vCpu * totalUtilization.AggDurationMSec * costProfile.vCpuPerHour / Units.Hour_Millisec;
            totalUtilization.MemoryCost = MemoryMB * totalUtilization.AggDurationMSec * costProfile.MemoryGBPerHour / Units.GB_MB / Units.Hour_Millisec;
            totalUtilization.NetworkCost = totalUtilization.InternetBandwidthMB * costProfile.BandwidthCostPerGBInternet / Units.GB_KB +
                totalUtilization.IntranetBandwidthMB * costProfile.BandwidthCostPerGBIntranet;
            return totalUtilization;
        }

        private List<AggregatedUtilizaion> CombineUtilizations()
        {
            if (!Utilizations.Any())
                return new List<AggregatedUtilizaion>();

            var sortedUtilizations = Utilizations.OrderBy(u => u.StartTime).ToList();

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

