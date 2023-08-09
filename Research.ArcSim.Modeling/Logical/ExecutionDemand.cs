using System;

namespace Research.ArcSim.Modeling.Logical
{
    public enum ExectuionMode
    {
        Initiative,
        Reactive
    }

    public enum DemandLevel
    {
        Low,
        Medium,
        High
    }

    public class ExecutionDemand
	{
        public ExectuionMode ExectuionMode { get; set; }
		public ProcessingDemand PP { get; set; }
		public MemoryDemand MP { get; set; }
		public BandwidthDemand BP { get; set; }

        public ExecutionDemand() { }

        public ExecutionDemand(DemandLevel processingLevel, DemandLevel memoryLevel, DemandLevel bandwithLevel)
        {
            PP = new ProcessingDemand();
            PP.Set(processingLevel);
            MP = new MemoryDemand();
            MP.Set(memoryLevel);
            BP = new BandwidthDemand();
            BP.Set(bandwithLevel);
        }
	}
}

