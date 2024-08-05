using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
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
            controller.Connect();
        }

    }
}
