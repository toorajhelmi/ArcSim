using System;
namespace Research.ArcSim.Modeling.Simulation
{
	public class CostProfile
	{
		public double CpuCostvCpuSec { get; set; }
        public double MemoryCostPerGBHour { get; set; }
        public double BandwidthCostPerGB { get; set; }
    }
}

