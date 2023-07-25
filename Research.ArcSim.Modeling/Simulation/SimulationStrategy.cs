﻿using System;
namespace Research.ArcSim.Modeling.Simulation
{
    //public enum SimulationOptimizationGoal
    //{
    //    MinCost,
    //    MinTime
    //}

    public enum RequestDistribution
    {
        Uniform,
        Bursty
    }

    public class SimulationStrategy
    {
        //public SimulationOptimizationGoal AllocationOptimizationGoal { get; set; }
        public int AverageProcessingTimeMillisec { get; set; }
        public int MaxResponseTime { get; set; }
        public int TotalCost { get; set; }
        public RequestDistribution RequestDistribution { get; set; }
        public int AvgReqPerSecond { get; set; }
        public int SimulationDurationSecs { get; set; }
    }
}

