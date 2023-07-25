namespace Research.ArcSim.Modeling
{
    public class Function
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ActivityDefinition> Activities { get; set; } = new List<ActivityDefinition>();
        public Module Module { get; set; }
    }
}