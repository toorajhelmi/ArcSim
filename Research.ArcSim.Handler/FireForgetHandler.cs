using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Handler;

public class FireForgetHandler : IHandler
{
    private HandlingStrategy handlingStrategy;
    private SimulationStrategy simulationStrategy;

    public static IHandler Instance { get; private set; }

    public static void Create(SimulationStrategy simulationStrategy, HandlingStrategy handlingStrategy)
    {
        Instance = new FireForgetHandler
        {
            handlingStrategy = handlingStrategy,
            simulationStrategy = simulationStrategy
        };
    }

    public void Handle(Request request)
    {
        Handle(request, new List<Request>());
    }

    public void Handle(Request request, List<Request> dependentRequests)
    {
        var cn = Allocator.Allocator.Instance.Allocate(request);
        if (cn == null)
        {
            request.ServingActivity.Failed = true;
            request.ServingActivity.FailureType = FailureType.ResourceUnavailable;
            return;
        }

        var statTime = Simulation.Instance.Now;
        var pendingDependencies = request.ServingActivity.Dependencies.Where(d => !d.Completed).ToList();
        if (pendingDependencies.Any())
        {
            dependentRequests.Add(request);

            foreach (var pendingDependency in pendingDependencies)
            {
                var dependentReq = new Request
                {
                    ServingActivity = pendingDependency,
                    RequestingActivity = request.ServingActivity
                };
                Handle(dependentReq, dependentRequests);
            }

            if (pendingDependencies.Any(r => r.Expired))
            {
                request.ServingActivity.Expired = true;
                return;
            }

            if (request.ServingActivity.Dependencies.Any(d => d.Failed))
            {
                request.ServingActivity.Failed = true;
                request.ServingActivity.FailureType = FailureType.DependencyFailed;
                return;
            }

            //var newStartTime = pendingDependencies.Max(d => d.EndTime);
            //if (newStartTime > request.ServingActivity.StartTime)
            //{
            //    Simulation.Instance.ScheduleRequest(newStartTime, request);

            //    foreach (var dependent in dependentRequests)
            //    {
            //        if (dependent.ServingActivity.StartTime < newStartTime)
            //        {
            //            Simulation.Instance.ScheduleRequest(newStartTime, dependent);
            //        }
            //    }
            //}

            statTime = pendingDependencies.Max(d => d.EndTime);
        }

        var (response, utilization) = cn.CalculateProcessingTimeMillisec(request);
        if (response.ProcessingTime == double.MaxValue || handlingStrategy.SkipExpiredRequests &&
            Simulation.Instance.Now + response.ProcessingTime > request.ServingActivity.StartTime + simulationStrategy.MaxResponseTime)
        {
            request.ServingActivity.Expired = true;
        }
        else
        {
            request.ServingActivity.EndTime = statTime + response.ProcessingTime;
            request.ServingActivity.Succeeded = response.Succeeded;
        }
    } 
}

