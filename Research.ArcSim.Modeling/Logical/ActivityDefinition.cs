using Research.ArcSim.Modeling.Logical;

namespace Research.ArcSim.Modeling
{
    public class Layer
    {
        public const string Client = "Client";
        public const string UI = "UI";
        public const string API = "API";
        public const string DB = "DB";
        public const string Custom = "Custom";
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
        public string Layer { get; set; }
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