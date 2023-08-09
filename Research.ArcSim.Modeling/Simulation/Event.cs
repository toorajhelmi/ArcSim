using System;
using Research.ArcSim.Modeling.Physical;

namespace Research.ArcSim.Modeling.Simulation
{
    public enum EventType
    {
        ActivityCompleted,
    }

    public class Event
	{
        public EventType EventType { get; set; }
		public string Description { get; set; }
		public ComputingNode Node { get; set; }
	}
}

