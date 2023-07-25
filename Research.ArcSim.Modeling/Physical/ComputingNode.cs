using System;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Modeling
{
    public class NodeConfig
    {
        public double vCpu { get; set; }
        public int MemoryMB { get; set; }
        public int BandwidthKBPerSec { get; set; }
    }

    public class ComputingNode : Node
	{
        public ComputingNode(NodeConfig config, Component component)
        {
            this.Config = config;
            Component = component;
        }

        //public List<Neighbor> Neighbors { get; set; } = new();
        public Component Component { get; set; }
        public NodeConfig Config { private set; get; }

        public int EstimateProcessingTimeMillisec(Activity request)
        {
            return (int)(request.Definition.ExecutionProfile.PP.DemandMilliCpuSec / Config.vCpu);
        }

        public void Process(Activity servingActivity, Activity requestingActivity)
        {
            servingActivity.EndTime = Simulation.Simulation.Instance.Now +
                EstimateProcessingTimeMillisec(servingActivity);

            if (servingActivity.Definition.Component != requestingActivity.Definition.Component)
                servingActivity.EndTime += 1000 * servingActivity.Definition.ExecutionProfile.BP.DemandKB / Config.BandwidthKBPerSec;

            //Simulation.Simulation.Instance.AddEvent(servingActivity.EndTime, this,
            //    EventType.ActivityCompleted, servingActivity.Id.ToString());
        }
    }
}

