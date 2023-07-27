using System;
namespace Research.ArcSim.Modeling.Logical
{
	public class MemoryProfile
	{
		public int DemandMB { get; set; }

        public void Set(DemandLevel demandLevel)
        {
            switch (demandLevel)
            {
                // DB Queries, Media Processing; Examples: Product Image Processing, Complex Database Queries 
                case DemandLevel.High: DemandMB = 5000; break;
                // State Management; Examples: Shopping Cart Management, User Session Management 
                case DemandLevel.Medium: DemandMB = 500; break;
                // API calls; Examples: Basic Product Listing, Checkout Process
                case DemandLevel.Low: DemandMB = 50; break;
            }
        }
    }
}

