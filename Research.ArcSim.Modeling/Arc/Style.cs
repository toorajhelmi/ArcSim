using System;
namespace Research.ArcSim.Modeling.Arc
{
    public enum ServerStyle
    {
        Microservices,
        Serverless,
        Layered,
        //Peer2Peer,
        Monolith
    }

    public enum ClientStyle
    {
        Web,
        Mobile,
        Desktop
    }

    public class Style
	{
		public ServerStyle ServerStyle { get; set; }
        public ClientStyle ClientStyle { get; set; }
	}
}

