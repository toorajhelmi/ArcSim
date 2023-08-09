using System;
using Research.ArcSim.Modeling.Logical;
using Research.ArcSim.Modeling.Physical;

namespace Research.ArcSim.Modeling.Simulation
{
    public class Simulation
    {
        public static Simulation Instance { get; private set; }

        private SortedDictionary<int, List<Event>> events { get; set; } = new();
        public int Now { get; set; } = new();
        public bool Terminated { get; set; }
        public Tuple<string, Activity> TerminationReason { get; set; }
        public SortedDictionary<int, List<Request>> requests = new();

        public static void Create()
        {
            Instance = new Simulation();
        }

        public void AddEvent(int time, ComputingNode node, EventType eventType, string description)
        {
            if (!events.ContainsKey(time))
            {
                events.Add(time, new List<Event>());
            }

            events[time].Add(new Event
            {
                Node = node,
                Description = description
            });
        }

        public void ScheduleRequest(int startTime, Request request)
        {
            if (!requests.ContainsKey(startTime))
                requests.Add(startTime, new List<Request>());

            request.ServingActivity.StartTime = startTime;
           
            requests[startTime].Add(request);
        }

        public void Terminate(string reason, Activity request)
        {
            TerminationReason = new Tuple<string, Activity>(reason, request);
            Terminated = true;
        }

        public List<Event> GetEvents(int time)
        {
            if (events.ContainsKey(time))
            {
                return events[time];
            }
            else return new List<Event>();
        }
    }
}

