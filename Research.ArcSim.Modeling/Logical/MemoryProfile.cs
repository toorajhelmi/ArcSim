using System;
namespace Research.ArcSim.Modeling.Logical
{
	public class MemoryProfile
	{
		public int DemandMB { get; set; }
        // How longer the task would take if it given a faction of demand. 1 mean it is linear so for example if it has access
        // to half the demandMB, it will take twice. Formula to calc time: AvailalbeMB > DemandMB => No change; Otherwise
        // DemandMB / AvailalbeMB
        public int TrashingFactor { get; set; } = 1;

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

