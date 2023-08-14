using System;
using Research.ArcSim.Modeling.Physical;

namespace Research.ArcSim.Modeling.Logical
{
	public enum FailureType
	{
		InternalError,
		ResourceUnavailable,
        DependencyFailed
    }

	public class Activity
	{
		static int nextId = 0;
		private int endT;
		public int Id { get; set; }
        public int AssignedCore { get; set; }

        public Activity(ActivityDefinition definition, int startTime)
		{
			Id = nextId++;
			Definition = definition;
			StartTime = startTime;

			Dependencies.AddRange(definition.Dependencies.Select(ad => new Activity(ad, startTime)));
		}

		public Activity Clone() => new Activity(Definition, 0);

		public bool Succeeded { get; set; }
		public bool Failed { get; set; }
		public bool Expired { get; set; }
		public bool Completed => Succeeded || Failed || Expired;
		public FailureType FailureType { get; set; }
		public ComputingNode Processor { get; set; }
		public List<Activity> Dependencies { get; set; } = new();
        public ActivityDefinition Definition { get; set; }
        public int StartTime { get; set; }
        public int EndTime
		{
			get => endT;
			set
			{
				if (value < 0)
				{
					;
				}
				else
					endT = value;
			}
		}
        public int ProcessingTime => EndTime - StartTime;

        public void ShowActivityTree(int depth = 0)
        {
            Console.WriteLine($"{new string(' ', depth)}ID: {Id}, S:{StartTime}, E:{EndTime}, C: {AssignedCore}");
            foreach (var child in Dependencies)
            {
                child.ShowActivityTree(depth + 1);
            }
        }

        public IEnumerable<Activity> Flatten()
        {
            yield return this;

            foreach (var dependent in Dependencies.SelectMany(d => d.Flatten()))
            {
                yield return dependent;
            }
        }
    }
}

