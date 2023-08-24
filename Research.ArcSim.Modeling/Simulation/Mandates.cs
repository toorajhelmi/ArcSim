using System;
using Research.ArcSim.Modeling.Logincal;

namespace Research.ArcSim.Modeling.Simulation
{
	public static class Mandates
	{
		private static Dictionary<(Type, Type), object> list { get; set; } = new();

		public static void Add<T, U>(Mandate<T, U> mandate)
		{
			list.Add((typeof(T), typeof(U)), mandate);
		}

		public static Mandate<T, U> Get<T, U>()
		{
			return list[(typeof(T), typeof(U))] as Mandate<T, U>;
		}
	}
}

