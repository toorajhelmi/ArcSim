namespace Research.ArcSim.Modeling
{
    //Component is an a horizontal or vertical cut and has a known operational requirment.
    //It can be installed on a node independently
    public class Component
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ActivityDefinition> Activities { get; set; } = new List<ActivityDefinition>();
    }
}