using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamerWinui
{
    internal class Test
    {
        public static void Main()
        {
            StreamSession streamSession = new StreamSession();
            //string format = Console.ReadLine();
            streamSession.startStream("hevc_nvenc", "mov");
        }
    }
}
