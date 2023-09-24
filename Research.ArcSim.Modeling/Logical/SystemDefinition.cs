using System;
using System.Text.Json.Serialization;

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

    public enum Parallelization
    {
        InterActivity,
        IntraActivity,
        Both,
        None
    }

	public class SystemDefinition
	{
        public string Name { get; set; } = "Tiny System";
        public int ModuleCount { get; set; } = 3;
        public int AvgfunctionsPerModule { get; set; } = 3;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModuleDependency InterModularDependency { get; set; }
        public bool IntraModularDependency { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Parallelization ActivityParallelization { get; set; }
    }
}

