using System;
using System.Linq;

namespace Research.ArcSim.Modeling.Simulation
{
	public class Mandate<T, U> 
	{
		private List<(T, U)> mandates = new();
		private U defaultValue { get; set; }

		public Mandate(List<(T, U)> mandates, U defaultValue)
		{
			this.mandates = mandates;
			this.defaultValue = defaultValue;
		}

		public U SetFor(T t)
		{
			if (!mandates.Any(pair => pair.Item1.Equals(t)))
				return defaultValue;
			else
				return mandates.First(pair => pair.Item1.Equals(t)).Item2;
		}
	}
}

