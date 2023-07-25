namespace Research.ArcSim.Modeling
{
    public class Module
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Function> Functions { get; set; } = new();
        public List<Module> Dependencies { get; set; } = new();
    }
}