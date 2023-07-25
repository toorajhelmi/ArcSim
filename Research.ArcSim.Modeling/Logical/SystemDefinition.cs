using System;
namespace Research.ArcSim.Modeling.Logical
{
    public enum ModuleDependency
    {
        None,
        Low,
        Medium,
        High,
        Extreme,
    }
	public class SystemDefinition
	{
        public string Name { get; set; } = "Tiny System";
        public int ModuleCount { get; set; } = 3;
        public int AvgfunctionsPerModule { get; set; } = 3;
        public ModuleDependency ModuleDependency { get; set; } = ModuleDependency.None;

    }
}

