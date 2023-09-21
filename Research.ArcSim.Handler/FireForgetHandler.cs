using Research.ArcSim.Allocators;
using Research.ArcSim.Modeling.Arc;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Handler;

public class FireForgetHandler : IHandler
{
    private HandlingStrategy handlingStrategy;
    private SimulationStrategy simulationStrategy;
    private Arch arch;
    private SystemDefinition systemDefinition;

    public static IHandler Instance { get; private set; }

    public static void Create(SimulationConfig simulationConfig)
    {
        Instance = new FireForgetHandler
        {
            handlingStrategy = simulationConfig.HandlingStrategy,
            simulationStrategy = simulationConfig.SimulationStrategy,
            arch = simulationConfig.Arch,
            systemDefinition = simulationConfig.SystemDefinition
        };
    }

    public void Handle(Request request)
    {
        var cn = Allocator.Instance.GetServingNode(request);
        if (cn == null)
        {
            request.ServingActivity.Failed = true;
            request.ServingActivity.FailureType = FailureType.ResourceUnavailable;
            return;
        }

        var utilization = cn.StartProcessing(request, systemDefinition.ActivityParallelization);

        var pendingDependencies = request.ServingActivity.Dependencies.Where(d => !d.Completed).ToList();
        if (pendingDependencies.Any())
        {
            foreach (var pendingDependency in pendingDependencies)
            {
                var dependentReq = new Request
                {
                    ServingActivity = pendingDependency,
                    RequestingActivity = request.ServingActivity
                };
                Handle(dependentReq);
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

            request.RequestedStartTime = pendingDependencies.Max(d => d.EndTime);
        }

        var response = cn.CompleteProcessing(request, utilization);
        request.ServingActivity.StartTime = utilization.StartTime;

        if (utilization.TotalMSec == double.MaxValue || handlingStrategy.SkipExpiredRequests &&
            utilization.EndTime > request.ServingActivity.StartTime + simulationStrategy.MaxResponseTime)
        {
            request.ServingActivity.Expired = true;
        }
        else
        {
            request.ServingActivity.EndTime = utilization.EndTime;
            request.ServingActivity.Succeeded = response.Succeeded;
        }
    } 
}

