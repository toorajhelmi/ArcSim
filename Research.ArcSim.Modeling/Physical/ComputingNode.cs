using System;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Modeling
{
    public class ComputingNode : Node
	{
        public double vCpu { get; set; }
        public double MemoryMB { get; set; }
        public double BandwidthKBPerSec { get; set; }
        // How longer the task would take if it given a faction of demand. 1 mean it is linear so for example if it has access
        // to half the demandMB, it will take twice. Formula to calc time: AvailalbeMB > DemandMB => No change; Otherwise
        // DemandMB / AvailalbeMB
        public int TrashingFactor { get; set; } = 1;

        public void AssignComponent(Component component)
        {
            Component = component;
        }

        //public List<Neighbor> Neighbors { get; set; } = new();
        public Component Component { get; set; }

        public int EstimateProcessingTimeMillisec(Activity servingActivity, Activity requestingActivity)
        {
            var trashingSlowness = 0.0;

            if (Component.RequiredMemoryMB > MemoryMB)
            {
                //In this case there is a chance that we need to load the required pages from disk. To caculate the time is takes
                //to load the page, we consider the chance of missing the page in memory and multiply that by the time is takes to
                //load from disk. 

                var swapProbability = 1 - MemoryMB / Component.RequiredMemoryMB;
                //We assume a throughput of 1200MB/S which is Premium SSD v2 in Azure
                //https://learn.microsoft.com/en-us/azure/virtual-machines/disks-types

                var diskThroughput = 1200;
                trashingSlowness = 1000 * swapProbability * servingActivity.Definition.ExecutionProfile.MP.DemandMB / diskThroughput;
            }

            var processingTime = (int)(servingActivity.Definition.ExecutionProfile.PP.DemandMilliCpuSec / vCpu) + trashingSlowness;
            if (requestingActivity.Definition.Component == null) // this means external call such as internet
            {
                processingTime += 1000 * servingActivity.Definition.ExecutionProfile.BP.DemandKB / BandwidthKBPerSec;
            }

            return (int)processingTime;
        }
    }
}

