using System;
using Research.ArcSim.Modeling.Arc;

namespace Research.ArcSim.Modeling.Core
{
	public class LogicalImplementation
	{
		public System System { get; set; }
		public Arch Arch { get; set; }
		public List<Component> Components { get; set; } = new();
	}
}

