using System;
namespace Research.ArcSim.Modeling.Logical
{
	public class BandwidthProfile
	{
		public int DemandKB { get; set; }

        public void Set(DemandLevel demandLevel)
        {
            switch (demandLevel)
            {
                //  Serving content; Examples, Product Image and Media Delivery, File Downloads
                case DemandLevel.High: DemandKB = 5000; break;
                // Web site rendreing; Examples: Web Page Loading, Shopping Cart Updates
                case DemandLevel.Medium: DemandKB = 500; break;
                // API calls; Examples: Basic Product Listing, User Authentication
                case DemandLevel.Low: DemandKB = 5; break;
            }
        }
    }
}

