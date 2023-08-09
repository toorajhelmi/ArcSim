using System;
using Research.ArcSim.Modeling.Logical;

namespace Research.ArcSim.Modeling.Physical
{
	public class Bandwidth
	{
		private BandwidthProfile internet;
		private BandwidthProfile intranet;


        public Bandwidth(BandwidthProfile internet, BandwidthProfile intranet)
		{
			this.internet = internet;
			this.intranet = intranet;
		}

		public double GetKBPerSec(Request request)
		{
			if (request.RequestingActivity is ExternalActivity)
				return internet.GetKBPerSec();
			if (request.RequestingActivity.Definition.Component == request.ServingActivity.Definition.Component)
				return double.MaxValue;
			else
				return intranet.GetKBPerSec();
		}
	}
}

