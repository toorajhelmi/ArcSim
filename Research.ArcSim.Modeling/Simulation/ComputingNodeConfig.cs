using System;

namespace Research.ArcSim.Modeling.Simulation
{
    public enum Nic
    {
        CpuIndependent,
        CpuDependent
    }

    public enum Sku
    {
        Specific,
        Range
    }

    public enum Location
    {
        OnPremise,
        Cloud
    }

	public class ComputingNodeConfig
	{
        public Nic Nic { get; set; }
        public Sku Sku { get; set; }
        public Location Location { get; set; }
    }
}

