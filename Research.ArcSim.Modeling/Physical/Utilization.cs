using System;
using Research.ArcSim.Modeling.Logical;

namespace Research.ArcSim.Modeling.Physical
{
    public class AggregatedUtilizaion
    {
        public int StartTime { get; set; }
        public virtual int EndTime { get; set; }
        public virtual int TotalMSec => EndTime - StartTime;
        public int AggDurationMSec { get; set; }
        public double CpuCost { get; set; }
        public double MemoryCost { get; set; }
        public double NetworkCost { get; set; }
        public double TotalCost => CpuCost + MemoryCost + NetworkCost;

        public double InternetBandwidthMB { get; set; }
        public double IntranetBandwidthMB { get; set; }
        public double LocalBandwidthMB { get; set; }

        public bool Overlaps(Utilization other)
        {
            return this.StartTime < other.EndTime && this.EndTime >= other.StartTime;
        }

        public AggregatedUtilizaion Combine(Utilization other = null)
        {
            if (other == null)
                return new AggregatedUtilizaion
                {
                    StartTime = StartTime,
                    EndTime = EndTime,
                    InternetBandwidthMB = InternetBandwidthMB,
                    IntranetBandwidthMB = IntranetBandwidthMB,
                    LocalBandwidthMB = LocalBandwidthMB
                };
            else
                return new AggregatedUtilizaion
                {
                    StartTime = StartTime < other.StartTime ? StartTime : other.StartTime,
                    EndTime = EndTime > other.EndTime ? EndTime : other.EndTime,
                    InternetBandwidthMB = InternetBandwidthMB + other.InternetBandwidthMB,
                    IntranetBandwidthMB = IntranetBandwidthMB + other.IntranetBandwidthMB,
                    LocalBandwidthMB = LocalBandwidthMB + other.LocalBandwidthMB
                };
        }
    }

    public class Utilization : AggregatedUtilizaion
    {
        public Request Request { get; set; }
        public override int EndTime => StartTime + TotalMSec;
        public int SwapingMSec { get; set; }
        public int ProcessingMSec { get; set; }
        public int TransmissionMSec { get; set; }
        public override int TotalMSec => SwapingMSec + ProcessingMSec + TransmissionMSec;

        public Utilization(Request request)
        {
            Request = request;
        }
    }
}

