using Research.ArcSim.Modeling.Logical;

namespace Research.ArcSim.Modeling
{
    public enum Layer
    {
        Client,
        Presentation,
        API,
        DB
    }

    /// <summary>
    /// Activity is a unit of execution that can run on a node
    /// </summary>
    public class ActivityDefinition
    {
        static int nextId = 0;
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Component Host { get; set; }
        public Layer Layer { get; set; }
        public List<ActivityDefinition> Dependencies { get; set; } = new List<ActivityDefinition>();
        public ExecutionDemand ExecutionProfile { get; set; }
        public Function Function { get; set; }
        public Component Component { get; set; }

        public ActivityDefinition()
        {
            Id = nextId++;
            //Default execution profile
            ExecutionProfile = new ExecutionDemand(DemandLevel.Medium, DemandLevel.Medium, DemandLevel.Medium);
        }
    }
}