using System.Collections.ObjectModel;
using Prism.Mvvm;
using Research.ArcSim.Extensions;
using Research.ArcSim.Modeling.Simulation;

namespace Research.ArcSim.Desktop.ViewModels
{
    public class Request
    {
        public string Id { get; set; }
        public Request Parent { get; set; }
        public string Notes { get; set; }
        public List<Request> Dependencies { get; set; } = new();
        public int Start
        {
            get; set;
        }
    }

    public class Allocation
    {
        public bool Allocated { get; set; }
        public int CoreIndex { get; set; }
        public string RequestId { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class NodeAllocation
    {
        public int NodeId { get; set; }
        public int CoreCount { get; set; }
        public ObservableCollection<Allocation> Allocations { get; set; } = new();
        public ObservableCollection<double> CpuUtilization { get; set; }
        public ObservableCollection<double> Cost { get; set; }
    }

    public class RequestAtTime
    {
        public Request Request { get; set; }
        public bool Exists { get; set; }
    }

    public class ResultsViewModel : BindableBase
    {
        private const int defaultReqPerTime = 10;
        private static ResultsViewModel instance;
        private List<NodeResult> nodes = new();
        private List<CoreUtil> utils = new();
        private List<Request> requests = new();

        public ObservableCollection<NodeAllocation> NodeAllocations { get; set; } = new();
        public ObservableCollection<int> TimeRange { get; set; } = new();
        public ObservableCollection<RequestAtTime> Requests { get; set; } = new();

        public int MinTime { get; set; }
        public int MaxTime { get; set; }
        public int TimeUnit { get; set; } = 1000;
        public int ReqsPerTime { get; set; } = defaultReqPerTime;
        public Command ApplyCommnad { get; set; }

        public static ResultsViewModel Instance => instance;

        static ResultsViewModel()
        {
            instance = new ResultsViewModel();
        }

        private ResultsViewModel()
        {
            MaxTime = TimeUnit * 20;
            ApplyCommnad = new Command(Visualize);
            //GenerateSampleData();
        }

        public void LoadResults(List<SimulationResult> simulationResults, List<Request> requests)
        {
            utils = simulationResults.First().CoreUtils;
            nodes = simulationResults.First().NodeResults;
            this.requests = requests;
        }

        private void Visualize()
        {
            ReqsPerTime = defaultReqPerTime;
            RaisePropertyChanged(nameof(ReqsPerTime));

            NodeAllocations.Clear();
            Requests.Clear();
            TimeRange.Clear();

            var activeNodes = nodes.Where(n => n.Start >= MinTime && n.Start <= MaxTime);

            foreach (var node in activeNodes)
            {
                var nodeAlloc = new NodeAllocation
                {
                    NodeId = node.NodeId,
                    CoreCount = node.CoreCount,
                    CpuUtilization = new(),
                    Cost = new()
                };

                NodeAllocations.Add(nodeAlloc);

                var nodeUtils = utils.Where(u => u.NodeId == node.NodeId).OrderBy(u => u.Start);

                for (int time = MinTime; time <= MaxTime; time += TimeUnit)
                {
                    for (var coreIndex = 0; coreIndex < node.CoreCount; coreIndex++)
                    {
                        var coreUtils = nodeUtils.Where(u => u.CoreIndex == coreIndex);

                        var coreUtil = coreUtils.FirstOrDefault(u => u.End >= time && u.Start < time + TimeUnit);
                        if (coreUtil != null)
                        {
                            nodeAlloc.Allocations.Add(new Allocation
                            {
                                Allocated = true,
                                CoreIndex = coreUtil.CoreIndex,
                                RequestId = coreUtil.RequestId,
                            });
                        }
                        else
                        {
                            nodeAlloc.Allocations.Add(new Allocation
                            {
                                Allocated = false
                            });
                        }
                    }

                    var cpuUtils = node.CpuUtilization.Where(cu => cu.Time >= time && cu.Time < time + TimeUnit);
                    if (cpuUtils.Any())
                        nodeAlloc.CpuUtilization.Add(cpuUtils.Average(cu => cu.Util));
                    else
                        nodeAlloc.CpuUtilization.Add(0);

                    var costs = node.Cost.Where(c => c.Time >= time && c.Time < time + TimeUnit);
                    if (costs.Any())
                        nodeAlloc.Cost.Add(costs.Average(c => c.Amount));
                    else
                        nodeAlloc.Cost.Add(0);
                }
            }

            for (var time = MinTime; time <= MaxTime; time += TimeUnit)
            {
                TimeRange.Add(time);

                var reqsAtTime = requests.Where(r => r.Start >= time && r.Start < time + TimeUnit);

                if (reqsAtTime.Count() > ReqsPerTime)
                {
                    ReqsPerTime = reqsAtTime.Count();
                }

                Requests.AddRange(reqsAtTime.Select(r => new RequestAtTime
                {
                    Request = r,
                    Exists = true,
                }));

                for (int i = 0; i < ReqsPerTime - reqsAtTime.Count(); i++)
                {
                    Requests.Add(new RequestAtTime());
                }
            }
        }

        private void GenerateSampleData()
        {
            var r = new Random();

            for (var nodeId = 0; nodeId < 3; nodeId++)
            {
                var node = new NodeResult
                {
                    NodeId = nodeId,
                    Start = r.Next(20),
                    End = 20 + r.Next(30),
                    CoreCount = r.Next(3) + 1
                };

                nodes.Add(node);

                for (var time = node.Start; time < node.End; time++)
                {
                    node.CpuUtilization.Add((time, r.NextDouble() * 100));
                    node.Cost.Add((time, r.NextDouble() * 10));
                }

                for (var coreIndex = 0; coreIndex < node.CoreCount; coreIndex++)
                {
                    for (var start = node.Start; start < node.End;)
                    {
                        var duration = r.Next(5) + 1;

                        var util = new CoreUtil
                        {
                            NodeId = nodeId,
                            CoreIndex = coreIndex,
                            Start = start,
                            End = start + r.Next(duration) + 1,
                            RequestId = r.Next(1000).ToString()
                        };

                        utils.Add(util);

                        requests.Add(new Request
                        {
                            Id = util.RequestId,
                            Start = util.Start,
                            Dependencies = new()
                        });

                        start += duration + 1;
                    }
                }
            }
        }
    }
}