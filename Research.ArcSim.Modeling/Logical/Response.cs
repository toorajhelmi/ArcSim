using System;
using Research.ArcSim.Modeling.Physical;

namespace Research.ArcSim.Modeling.Logical
{
	public class Response
	{
		public bool Succeeded { get; set; }

		public Response(bool succeeded)
		{
			Succeeded = succeeded;
        }
	}
}

