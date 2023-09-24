using System;
using System.Text.Json.Serialization;

namespace Research.ArcSim.Modeling.Simulation
{
    public enum Nic
    {
        CpuIndependent,
        CpuDependent
    }

    public enum Sku
    {
        Specific,
        Range
    }

    public enum Location
    {
        OnPremise,
        Cloud
    }

	public class ComputingNodeConfig
	{
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Nic Nic { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Sku Sku { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Location Location { get; set; }
    }
}

