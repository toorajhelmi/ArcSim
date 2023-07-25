using Research.ArcSim.Modeling.Logical;

namespace Research.ArcSim.Modeling
{
	public class System
	{
		public SystemDefinition SystemDefinition { get; set; }
		public List<System> SubSystems { get; set; } = new();
        public List<Module> Modules { get; set; } = new();
    }
}

