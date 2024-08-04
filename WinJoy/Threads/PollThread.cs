using Nefarius.ViGEm.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinJoy.Threads
{
    internal class PollThread
    {
        internal PollThread()
        {
            var client = new ViGEmClient();
            var controller = client.CreateXbox360Controller();
        }

    }
}
