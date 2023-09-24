using System;
using Research.ArcSim.Modeling.Logical;

namespace Research.ArcSim.Modeling.Physical
{
	public class Bandwidth
	{
		public BandwidthProfile Internet { get; set; }
		public BandwidthProfile Intranet { get; set; }

        public Bandwidth()
		{
		}

        public Bandwidth(BandwidthProfile internet, BandwidthProfile intranet)
		{
            Internet = internet;
			Intranet = intranet;
		}

		public double GetKBPerSec(Request request)
		{
			if (request.RequestingActivity is ExternalActivity)
				return Internet.GetKBPerSec();
			if (request.RequestingActivity.Definition.Component == request.ServingActivity.Definition.Component)
				return double.MaxValue;
			else
				return Intranet.GetKBPerSec();
		}
	}
}

