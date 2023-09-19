using System.Collections.ObjectModel;
using Prism.Mvvm;
using Research.ArcSim.Extensions;
using Research.ArcSim.Modeling.Physical;

namespace Research.ArcSim.Desktop.ViewModels
{
    public class Node
    {
        public int NodeId { get; set; }
        public List<(int Time, double Util)> CpuUtilization { get; set; } = new();
        public List<(int Time, double Amount)> Cost { get; set; } = new();
        public int Start { get; set; }
        public int End { get; set; }
        public int CoreCount { get; set; }
    }

    public class Util
    {
        public int NodeId { get; set; }
        public int CoreIndex { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public string RequestId { get; set; }
    }

    public class Request
    {
        public string Id { get; set; }
        public Request Parent { get; set; }
        public List<Request> Dependencies { get; set; } = new();
        public int Start
        {
            get; set;
        }

        //^^^ are for testing. These should be passed from the actual sim

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

        public class SimulationViewModel : BindableBase
        {
            private const int defaultReqPerTime = 5;
            private List<Node> nodes = new();
            private List<Util> utils = new();
            private List<Request> requests = new();

            public ObservableCollection<NodeAllocation> NodeAllocations { get; set; } = new();
            public ObservableCollection<int> TimeRange { get; set; } = new();
            public ObservableCollection<RequestAtTime> Requests { get; set; } = new();

            public int MinTime { get; set; }
            public int MaxTime { get; set; } = 20;
            public int TimeUnit { get; set; } = 2;
            public int ReqsPerTime { get; set; } = defaultReqPerTime;
            public Command ApplyCommnad { get; set; }

            public SimulationViewModel(List<ComputingNode> _nodes)
            {
                ApplyCommnad = new Command(Visualize);
                GenerateSampleData();
            }

            private void Visualize()
            {
                ReqsPerTime = defaultReqPerTime;
                RaisePropertyChanged(nameof(ReqsPerTime));

                NodeAllocations.Clear();
                Requests.Clear();
                TimeRange.Clear();

                var activeNodes = nodes.Where(n => n.Start >= MinTime && n.Start <= MaxTime);

                var newAllocations = new List<NodeAllocation>();

                foreach (var node in activeNodes)
                {
                    var nodeAlloc = new NodeAllocation
                    {
                        NodeId = node.NodeId,
                        CoreCount = node.CoreCount,
                        CpuUtilization = new(),
                        Cost = new()
                    };

                    newAllocations.Add(nodeAlloc);

                    var nodeUtils = utils.Where(u => u.NodeId == node.NodeId).OrderBy(u => u.Start);

                    for (int time = MinTime; time <= MaxTime; time += TimeUnit)
                    {
                        for (var coreIndex = 0; coreIndex < node.CoreCount; coreIndex++)
                        {
                            var coreUtils = nodeUtils.Where(u => u.CoreIndex == coreIndex);

                            var coreUtil = coreUtils.FirstOrDefault(u => u.Start >= time && u.Start < time + TimeUnit);
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

                NodeAllocations.AddRange(newAllocations);

                var newRequests = new List<RequestAtTime>();

                for (var time = MinTime; time <= MaxTime; time += TimeUnit)
                {
                    TimeRange.Add(time);

                    var reqsAtTime = requests.Where(r => r.Start >= time && r.Start < time + TimeUnit);

                    if (reqsAtTime.Count() > ReqsPerTime)
                    {
                        ReqsPerTime = reqsAtTime.Count();        
                    }

                    newRequests.AddRange(reqsAtTime.Select(r => new RequestAtTime
                    {
                        Request = r,
                        Exists = true,
                    }));

                    for (int i = 0; i < ReqsPerTime - reqsAtTime.Count(); i++)
                    {
                        newRequests.Add(new RequestAtTime());
                    }
                }

                Requests.AddRange(newRequests);
                RaisePropertyChanged(nameof(ReqsPerTime));
            }

            private void GenerateSampleData()
            {
                var r = new Random();

                for (var nodeId = 0; nodeId < 3; nodeId++)
                {
                    var node = new Node
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

                            var util = new Util
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
}