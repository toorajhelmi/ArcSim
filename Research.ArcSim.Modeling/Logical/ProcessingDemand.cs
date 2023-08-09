using System;
namespace Research.ArcSim.Modeling.Logical
{
	public class ProcessingDemand
	{
		//How many seconds the task will take if it has access to 1GHz CPU
		public int DemandMilliCpuSec { get; set; }

        public void Set(DemandLevel demandLevel)
        {
            switch (demandLevel)
            {
                // Analytics;  Examples: Real-time Product Recommendations, Intensive Data Analytics 
                case DemandLevel.High: DemandMilliCpuSec = 5000; break;
                // API calls with calc; Examples: Shopping Cart Calculations, User Authentication and Authorization 
                case DemandLevel.Medium: DemandMilliCpuSec = 500; break;
                // API calls without calc, Content sercing; Examples: Static Content Serving, Basic Product Listing
                case DemandLevel.Low: DemandMilliCpuSec = 50; break;
            }
        }
    }
}

