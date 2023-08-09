using Research.ArcSim.Modeling.Logical;

namespace Research.ArcSim.Handler
{
    public interface IHandler
    {
        void Handle(Request request);
    }
}