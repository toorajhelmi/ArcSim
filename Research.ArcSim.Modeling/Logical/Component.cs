using Research.ArcSim.Modeling.Physical;

namespace Research.ArcSim.Modeling
{
    //Component is an a horizontal or vertical cut and has a known operational requirment.
    //It can be installed on a node independently
    public class Component
    {
        private List<ActivityDefinition> activities = new();
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ActivityDefinition> Activities
        {
            get => activities;
            set
            {
                activities = value;
                RequiredMemoryMB = activities.Sum(a => a.ExecutionProfile.MP.DemandMB);
            }
        } 

        public int RequiredMemoryMB { get; set; }
        public ComputingNode AssignedNode { get; set; }
    }
}