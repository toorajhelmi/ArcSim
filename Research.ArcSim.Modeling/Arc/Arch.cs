using System;
namespace Research.ArcSim.Modeling.Arc
{
    public enum DeploymentStyle
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

    public enum RequestRouting
    {
        API,
        P2P
    }

    public enum ProcessingMode
    {
        FireForget,
        Queued
    }

    public class Arch
	{
		public DeploymentStyle DeploymentStyle { get; set; }
        public ClientStyle ClientStyle { get; set; }
        public RequestRouting RequestRouting { get; set; }
        public ProcessingMode ProcessingMode { get; set; }
	}
}

