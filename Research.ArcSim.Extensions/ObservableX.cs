using System.Collections.ObjectModel;

namespace Research.ArcSim.Extensions
{
    public static class ObservableEx
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> list)
        {
            foreach (var item in list)
            {
                collection.Add(item);
            }
        }
    }
}

