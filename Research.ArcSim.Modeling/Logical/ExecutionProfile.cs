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

    public class ExecutionProfile
	{
        public ExectuionMode ExectuionMode { get; set; }
		public ProcessingProfile PP { get; set; }
		public MemoryProfile MP { get; set; }
		public BandwidthProfile BP { get; set; }

        public ExecutionProfile() { }

        public ExecutionProfile(DemandLevel processingLevel, DemandLevel memoryLevel, DemandLevel bandwithLevel)
        {
            PP = new ProcessingProfile();
            PP.Set(processingLevel);
            MP = new MemoryProfile();
            MP.Set(memoryLevel);
            BP = new BandwidthProfile();
            BP.Set(bandwithLevel);
        }
	}
}

