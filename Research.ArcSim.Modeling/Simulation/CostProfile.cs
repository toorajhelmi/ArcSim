namespace Research.ArcSim.Modeling.Simulation
{
    public class CostProfile
	{
		public double vCpuPerHour { get; set; }
        public double MemoryGBPerHour { get; set; }
        public double BandwidthCostPerGBInternet { get; set; }
        public double BandwidthCostPerGBIntranet { get; set; }
    }
}

