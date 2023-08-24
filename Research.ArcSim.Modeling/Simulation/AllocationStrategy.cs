using System;
using Research.ArcSim.Modeling.Arc;

namespace Research.ArcSim.Modeling.Simulation
{
	public enum Stickiness
	{
		OnDemand,
		Upfront
	}

	public enum HorizonalScaling
	{
		None,
		CpuControlled,
		QueueControlled
	}

	public enum LoadBalancingStrategy
	{
		RoundRobin,
		LeastUtilized,
		LeastReponseTime
	}

	public class HorizontalScalingConfig
	{
        public HorizonalScaling HorizonalScaling { get; set; }
		public int MinCpuUtilization { get; set; }
		public int MaxCpuUtilization { get; set; }
		public int MinQueueLength { get; set; }
        public int MaxQueueLength { get; set; }
		public int CooldownPeriod { get; set; }
		public int DefaultInstance { get; set; }
		public int MaxInstances { get; set; }
		public int MinInstances { get; set; }
		public LoadBalancingStrategy LoadBalancingStrategy { get; set; }
    }

    public class AllocationStrategy
	{
		public Mandate<DeploymentStyle, Stickiness> Stickiness { get; set; }
		public HorizontalScalingConfig HorizontalScalingConfig { get; set; } = new();
	}
}

