namespace Research.ArcSim.Modeling
{
    public class Node
    {
        static int id = 0;
        public int Id { get; set; }
        public int NetworkId { get; set; }
        public int RegionId { get; set; }

        public Node() => Id = id++;
    }
}