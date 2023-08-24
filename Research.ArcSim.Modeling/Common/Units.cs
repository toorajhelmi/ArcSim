using System;
namespace Research.ArcSim.Modeling.Common
{
	public class Units
	{
        public const int Min_Sec = 60;
        public const int Sec_Millisec = 1000;
        public const int Hour_Millisec = 60 * 60 * 1000;
        public const int GB_MB = 1024;
        public const int MB_KB = 1024;
        public const int GB_KB = GB_MB * MB_KB;
 
        public const int Minute = Min_Sec * Sec_Millisec;
    }
}

