using System;
namespace Research.ArcSim.Modeling.Physical
{
    public enum BandwidthPattern
    {
        Fixed,
        Uniform,
    }

    public class BandwidthProfile
	{
        public double MaxKBPerSec { get; set; }
        public double VariatianRatio { get; set; }
        public BandwidthPattern Pattern { get; set; }

        public BandwidthProfile(double maxKBPerSec)
        {
            Pattern = BandwidthPattern.Fixed;
            MaxKBPerSec = maxKBPerSec;
        }

        public BandwidthProfile(double maxKBPerSec, double variationRatio)
        {
            Pattern = BandwidthPattern.Uniform;
            MaxKBPerSec = maxKBPerSec;
            VariatianRatio = variationRatio;
        }

        public double GetKBPerSec()
        {
            if (Pattern == BandwidthPattern.Fixed)
                return MaxKBPerSec;
            else
            {
                var random = new Random();
                var bandwidth = random.Next((int)(VariatianRatio * MaxKBPerSec + 1)) + MaxKBPerSec * (1 - VariatianRatio);
                return bandwidth;
            }
        }
    }
}

