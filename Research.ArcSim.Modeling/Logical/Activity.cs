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

		public Activity(ActivityDefinition definition, int startTime)
		{
			Id = nextId++;
			Definition = definition;
			StartTime = startTime;
			OriginalStartTime = startTime;

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
        public int OriginalStartTime { get; set; }
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
        public int ProcessingTime => EndTime - OriginalStartTime;
    }
}

