using System;
using Research.ArcSim.Modeling.Common;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Modeling.Physical
{
    public class Utilization
    {
        public Request Request { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }

        public double InternetBandwidthMB { get; set; }
        public double IntranetBandwidthMB { get; set; }
        public double LocalBandwidthMB { get; set; }

        public double SwapingMSec { get; set; }
        public double ProcessingMSec { get; set; }
        public double TransmissionMSec { get; set; }
        public double TotalMSec => SwapingMSec + ProcessingMSec + TransmissionMSec;

        public double CpuCost { get; set; }
        public double MemoryCost { get; set; }
        public double NetworkCost { get; set; }
        public double TotalCost => CpuCost + MemoryCost + NetworkCost;

        public Utilization(Request request)
        {
            Request = request;
        }

        public bool Overlaps(Utilization other)
        {
            return this.StartTime < other.EndTime && this.EndTime > other.StartTime;
        }

        public Utilization Combine(Utilization other)
        {
            return new Utilization(null)
            {
                StartTime = StartTime < other.StartTime ? StartTime : other.StartTime,
                EndTime = EndTime > other.EndTime ? EndTime : other.EndTime,
                InternetBandwidthMB = InternetBandwidthMB + other.InternetBandwidthMB,
                IntranetBandwidthMB = IntranetBandwidthMB + other.IntranetBandwidthMB,
                LocalBandwidthMB = LocalBandwidthMB + other.LocalBandwidthMB
            };
        }
    }

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

        public (Response, Utilization) CalculateProcessingTimeMillisec(Request request, bool utilize = false)
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

            if (utilize)
            {
                utilization = new Utilization(request);
                utilization.StartTime = Simulation.Simulation.Instance.Now;
                utilization.EndTime = Simulation.Simulation.Instance.Now + (int)processingTime;
                utilization.SwapingMSec = swapingMSec;
                utilization.ProcessingMSec = request.ServingActivity.Definition.ExecutionProfile.PP.DemandMilliCpuSec / vCpu;
                utilization.TransmissionMSec = trasmissionMSec;
                if (request.GetScope() == RequestScope.Internet)
                    utilization.InternetBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB;
                else if (request.GetScope() == RequestScope.Internet)
                    utilization.IntranetBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB;
                else
                    utilization.LocalBandwidthMB = request.ServingActivity.Definition.ExecutionProfile.BP.DemandKB;


                Utilizations.Add(utilization);
            }

            return (new Response
            {
                Succeeded = true,
                ProcessingTime = (int)processingTime,
            }, utilization);
        }

        public Utilization GetUtilization(CostProfile costProfile)
        {
            var combinedUtilizations = CombineUtilizations();
            var totalUtilization = new Utilization(null)
            {
                SwapingMSec = combinedUtilizations.Sum(cu => cu.SwapingMSec),
                ProcessingMSec = combinedUtilizations.Sum(cu => cu.ProcessingMSec),
                TransmissionMSec = combinedUtilizations.Sum(cu => cu.TransmissionMSec),
                InternetBandwidthMB = combinedUtilizations.Sum(cu => cu.InternetBandwidthMB) / Units.MB_KB,
                IntranetBandwidthMB = combinedUtilizations.Sum(cu => cu.IntranetBandwidthMB) / Units.MB_KB,
                LocalBandwidthMB = combinedUtilizations.Sum(cu => cu.LocalBandwidthMB) / Units.MB_KB,
            };

            totalUtilization.CpuCost = vCpu * totalUtilization.ProcessingMSec * costProfile.vCpuPerHour / Units.Hour_Millisec;
            totalUtilization.MemoryCost = MemoryMB * totalUtilization.ProcessingMSec * costProfile.MemoryGBPerHour / Units.GB_MB / Units.Hour_Millisec;
            totalUtilization.NetworkCost = totalUtilization.InternetBandwidthMB * costProfile.BandwidthCostPerGBInternet / Units.GB_KB +
                totalUtilization.IntranetBandwidthMB * costProfile.BandwidthCostPerGBIntranet;
            return totalUtilization;
        }

        private List<Utilization> CombineUtilizations()
        {
            if (!Utilizations.Any())
                return new List<Utilization>();

            var sortedUtilizations = Utilizations.OrderBy(u => u.StartTime).ToList();

            var result = new List<Utilization>();
            var currentWindow = sortedUtilizations[0];

            for (int i = 1; i < sortedUtilizations.Count; i++)
            {
                if (currentWindow.Overlaps(sortedUtilizations[i]))
                {
                    currentWindow = currentWindow.Combine(sortedUtilizations[i]);
                }
                else
                {
                    result.Add(currentWindow);
                    currentWindow = sortedUtilizations[i];
                }
            }

            result.Add(currentWindow);
            return result;
        }
    }
}

