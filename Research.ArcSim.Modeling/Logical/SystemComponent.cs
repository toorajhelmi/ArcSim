namespace Research.ArcSim.Modeling.Logical
{
    public class SystemComponent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string HorizontalTag { get; set; }
        public string VerticalTag { get; set; }
        public int FunctionsCount { get; set; }
        public ExecutionDemand ExecutionDemand { get; set; }
        public DemandLevel Cpu { get; set; } = DemandLevel.Medium;
        public DemandLevel Mem { get; set; } = DemandLevel.Medium;
        public DemandLevel BW { get; set; } = DemandLevel.Medium;
    }
}

