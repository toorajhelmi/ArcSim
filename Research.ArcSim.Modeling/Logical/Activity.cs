﻿using System;
namespace Research.ArcSim.Modeling.Logical
{
	public class Activity
	{
		static int nextId = 0;
		public int Id { get; set; }

		public Activity(ActivityDefinition definition, int startTime)
		{
			Id = nextId++;
			Definition = definition;
			StartTime = startTime;
			OriginalStartTime = startTime;

			Dependencies.AddRange(definition.Dependencies.Select(ad => new Activity(ad, startTime)));
		}

		public bool Completed { get; set; }
		public bool Expired { get; set; }
		public ComputingNode Processor { get; set; }
		public List<Activity> Dependencies { get; set; } = new();
        public ActivityDefinition Definition { get; set; }
        public int OriginalStartTime { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public int ProcessingTime => EndTime - OriginalStartTime;
    }
}

