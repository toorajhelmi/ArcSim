using System;
namespace Research.ArcSim.Modeling.Logical
{
	public class ExternalActivity : Activity
	{ 
		public ExternalActivity() : base(new ActivityDefinition(), 0)  
		{
			Id = -1;
		}
	}
}

