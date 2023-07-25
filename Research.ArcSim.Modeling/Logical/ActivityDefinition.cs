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
        public string Name { get; set; }
        public string Description { get; set; }
        public Component Host { get; set; }
        public Layer Layer { get; set; }
        public List<ActivityDefinition> Dependencies { get; set; } = new List<ActivityDefinition>();
        public ExecutionProfile ExecutionProfile { get; set; }
        public Function Function { get; set; }
        public Component Component { get; set; }

        public ActivityDefinition()
        {
            //Default execution profile
            ExecutionProfile = new ExecutionProfile(DemandLevel.Medium, DemandLevel.Medium, DemandLevel.Medium);
        }
    }
}