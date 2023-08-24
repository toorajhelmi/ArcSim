using System;
using Research.ArcSim.Modeling.Arc;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Physical;

namespace Research.ArcSim.Modeling.Simulation
{
	public class SimulationConfig
	{
		public SystemDefinition SystemDefinition { get; set; }
        public AllocationStrategy AllocationStrategy { get; set; }
		public ComputingNodeConfig ComputingNodeConfig { get; set; }
		public HandlingStrategy HandlingStrategy { get; set; }
		public SimulationStrategy SimulationStrategy { get; set; }
		public Bandwidth Bandwidth { get; set; }
		public Arch Arch { get; set; }
	}
}

