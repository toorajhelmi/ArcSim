using System;
namespace Research.ArcSim.Modeling.Simulation
{
    public class CoreUtil
    {
        public int NodeId { get; set; }
        public int CoreIndex { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public string RequestId { get; set; }
    }

    public class NodeResult
    {
        public int NodeId { get; set; }
        public List<(int Time, double Util)> CpuUtilization { get; set; } = new();
        public List<(int Time, double Amount)> Cost { get; set; } = new();
        public int Start { get; set; }
        public int End { get; set; }
        public int CoreCount { get; set; }
    }

    public class SimulationResult
    {
        public string Descripton { get; set; }
        public SimulationConfig Conig { get; set; }
        public List<NodeResult> NodeResults { get; set; } = new();
        public List<CoreUtil> CoreUtils { get; set; } = new();
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public double SuccessRate => (double)CompletedRequests / TotalRequests;
    }
}

