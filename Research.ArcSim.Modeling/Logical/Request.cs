using System;
namespace Research.ArcSim.Modeling.Logical
{
	public enum RequestScope
	{
		Internet,
		Intranet,
		Local
	}

	public class Request
	{
		public int Id { get; set; }
		public Activity ServingActivity { get; set; }
        public Activity RequestingActivity { get; set; }
        public int TrialCount { get; set; }

        public RequestScope GetScope()
		{
			if (RequestingActivity is ExternalActivity)
				return RequestScope.Internet;
			else if (ServingActivity.Definition.Component == RequestingActivity.Definition.Component)
				return RequestScope.Local;
			else
				return RequestScope.Intranet;
		}
    }
}

