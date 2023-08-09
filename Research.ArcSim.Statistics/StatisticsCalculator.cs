namespace Research.ArcSim.Statistics;

public enum Stat
{
    Average,
    Percentage,
    Max,
    Min,
}

public class StatisticsCalculator<T>
{
    public static StatisticsCalculator<T> Instance { get; private set; }

    public static void Create() => Instance = new();

    private List<List<T>> data = new(); //Trials of times of values
     
    public void Log(Func<List<T>> getListData)
    {
        data.Add(getListData());
    }

    public double CalcStats(Func<T, bool> hasDesiredValue, Stat stat)
    {
        switch (stat)
        {
            case Stat.Average: return data.Average(trial => trial.Count(item => hasDesiredValue(item)));
            case Stat.Max: return data.Max(trial => trial.Count(item => hasDesiredValue(item)));
            case Stat.Min: return data.Min(trial => trial.Count(item => hasDesiredValue(item)));
            case Stat.Percentage: return 100 * data.Average(trial => (double)trial.Count(item => hasDesiredValue(item)) / trial.Count());
            default: return 0;
        }
    }

    public List<T> Any(Func<T, bool> condition)
    {
        return data.SelectMany(d => d).Where(s => condition(s)).ToList();
    }
}

