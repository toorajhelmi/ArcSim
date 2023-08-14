using System;
namespace Research.ArcSim.Modeling.Simulation
{
	public enum Stickiness
	{
		OnDemand,
		Upfront
	}

	public class AllocationStrategy
	{
		public Stickiness Stickiness { get; set; }
	}
}

