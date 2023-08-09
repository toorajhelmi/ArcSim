using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Physical;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Handler;

public class QueuedHandler : IHandler
{
    private HandlingStrategy handlingStrategy;
    private SimulationStrategy simulationStrategy;

    public static IHandler Instance { get; private set; }

    private Dictionary<int, Queue<Request>> processingQueue = new();
    private Dictionary<Request, ComputingNode> activeList = new();

    public static void Create(SimulationStrategy simulationStrategy, HandlingStrategy handlingStrategy)
    {
        Instance = new QueuedHandler
        {
            handlingStrategy = handlingStrategy,
            simulationStrategy = simulationStrategy
        };
    }

    public void Handle(Request request)
    {
        if (!processingQueue.ContainsKey(request.ServingActivity.Id))
            processingQueue.Add(request.ServingActivity.Id, new Queue<Request>());
;       processingQueue[request.ServingActivity.Id].Enqueue(request);
    }

    public void Run()
    {
        foreach (var kv in processingQueue)
        {
            if (kv.Value.Any())
            {
                while (true)
                {
                    var cn = Allocator.Allocator.Instance.Allocate(kv.Value.First());
                    if (cn != null)
                    {
                        var head = kv.Value.Dequeue();
                        Process(cn, head);
                    }
                    else
                        break;
                }
            }
        }

        var successList = new List<Request>();
        var failedList = new List<Request>();

        foreach (var request in activeList)
        {
            if (request.Key.ServingActivity.Succeeded)
                successList.Add(request.Key);
            else
            {
                failedList.Add(request.Key);
                if (request.Key.ServingActivity.Failed && handlingStrategy.TrialCount < request.Key.TrialCount)
                    Handle(request.Key);
            }

            var pendingDependencies = request.Key.ServingActivity.Dependencies.Where(d => !d.Completed).ToList();
            if (pendingDependencies.All(d => d.Completed && d.EndTime >= Simulation.Instance.Now))
            {
                CompleteProcessing(request.Key, request.Value);
            }

            successList.ForEach(r => activeList.Remove(r));
            failedList.ForEach(r => activeList.Remove(r));
        }
    }

    private void Process(ComputingNode cn, Request request)
    {
        request.TrialCount++;
        activeList.Add(request, cn);

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
                activeList.Remove(request);
                return;
            }
        }

        //if (request.ServingActivity.Dependencies.Any() && request.ServingActivity.Dependencies.All(d => d.Completed))
        //{
        //    var lastDependencyEndTime = request.ServingActivity.Dependencies.Max(d => d.EndTime);
        //    if (lastDependencyEndTime > request.ServingActivity.StartTime)
        //    {
        //        Simulation.Instance.ScheduleRequest(lastDependencyEndTime, request);
        //        return;
        //    }
        //}
    }

    private void CompleteProcessing(Request request, ComputingNode cn)
    {
        var (response, utilization) = cn.CalculateProcessingTimeMillisec(request);
        if (response.ProcessingTime == double.MaxValue || handlingStrategy.SkipExpiredRequests &&
            Simulation.Instance.Now + response.ProcessingTime > request.ServingActivity.StartTime + simulationStrategy.MaxResponseTime)
        {
            request.ServingActivity.Expired = true;
            request.ServingActivity.EndTime = Simulation.Instance.Now;
        }
        else
        {
            request.ServingActivity.EndTime = Simulation.Instance.Now + response.ProcessingTime;
            request.ServingActivity.Succeeded = true;
        }

        Allocator.Allocator.Instance.FreeUp(cn, request.ServingActivity.EndTime);
    }
}

