using System;
namespace Research.ArcSim.Modeling.Simulation
{
    public class HandlingStrategy
    {
        public bool SkipExpiredRequests { get; set; } = true;
        public int TrialCount { get; set; }
    }
}

