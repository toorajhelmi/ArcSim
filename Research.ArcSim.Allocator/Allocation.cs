using System;
using Research.ArcSim.Modeling;

namespace Research.ArcSim.Allocator
{
	public class Allocation
	{
		public ComputingNode ComputingNode { get; set; }
		public int From { get; set; }
		public int To { get; set; }
    }
}

