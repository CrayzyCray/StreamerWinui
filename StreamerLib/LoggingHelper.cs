#if TRACE_ON
#define TRACE_TST
#endif

using System.Diagnostics;

namespace StreamerLib
{
    internal static class LoggingHelper
    {
        static int threadID => Thread.CurrentThread.ManagedThreadId;
        static int lastThreadId = -1;

        [Conditional("TRACE_SL")]
        internal static void LogToCon(string msg)
        {
            string print = "";
            if (lastThreadId != threadID)
                print += "\n";
            print += $"    Thread{threadID}: {msg}";
            Debug.WriteLine(print);
            lastThreadId = threadID;
        }
    }
}
