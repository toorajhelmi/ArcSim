using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Handler;
public class Handler
{
    private HandlingStrategy handlingStrategy;
    private SimulationStrategy simulationStrategy;

    public static Handler Instance { get; private set; }

    public static void Create(SimulationStrategy simulationStrategy, HandlingStrategy handlingStrategy)
    {
        Instance = new Handler
        {
            handlingStrategy = handlingStrategy,
            simulationStrategy = simulationStrategy
        };
    }

    public void Handle(Activity servingActivity, Activity requestingActivity = null, List<Activity> dependents = default)
    {
        var pendingDependencies = servingActivity.Dependencies.Where(d => !d.Completed).ToList();
        if (pendingDependencies.Any())
        {
            dependents.Add(servingActivity);

            foreach (var pendingDependency in pendingDependencies)
            {
                Handle(pendingDependency, servingActivity, dependents);
            }

            if (pendingDependencies.Any(r => r.Expired))
            {
                servingActivity.Expired = true;
                return;
            }

            var newStartTime = pendingDependencies.Max(d => d.EndTime);
            if (newStartTime > servingActivity.StartTime)
            {
                Simulation.Instance.ScheduleRequest(newStartTime, servingActivity);

                foreach (var dependent in dependents)
                {
                    if (dependent.StartTime < newStartTime)
                    {
                        Simulation.Instance.ScheduleRequest(newStartTime, dependent);
                    }
                }
            }

            return;
        }

        if (servingActivity.Dependencies.Any() && servingActivity.Dependencies.All(d => d.Completed))
        {
            var lastDependencyEndTime = servingActivity.Dependencies.Max(d => d.EndTime);
            if (lastDependencyEndTime > servingActivity.StartTime)
            {
                Simulation.Instance.ScheduleRequest(lastDependencyEndTime, servingActivity);
                return;
            }
        }

        var cn = Allocator.Allocator.Instance.Allocate(servingActivity);
        if (cn != null)
        {
            var estimatedProcessingTime = cn.EstimateProcessingTimeMillisec(servingActivity);
            if (handlingStrategy.SkipExpiredRequests &&
                Simulation.Instance.Now + estimatedProcessingTime > servingActivity.StartTime + simulationStrategy.MaxResponseTime)
            {
                servingActivity.Expired = true;
            }
            else
            {
                cn.Process(servingActivity, requestingActivity);
                servingActivity.Completed = true;
            }
        }
    }
}

