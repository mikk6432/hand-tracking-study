using System;

namespace Utils
{
    public static class TimeMeasurementHelper
    {
        public static DateTime GetHighResolutionDateTime()
        {
            // TODO: ask if this is enough
            return DateTime.Now;
        }
    }
    
    /*// eyegazeonthego
    public static class TimeMeasurementHelper
    {
        // Based on https://manski.net/2014/07/high-resolution-clock-in-csharp/ by Sebastian Krysmanski
        public static DateTime GetHighResolutionDateTime()
        {
            GetSystemTimePreciseAsFileTime(out long fileTime);
            return DateTime.FromFileTimeUtc(fileTime);
        }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);
    }*/
}