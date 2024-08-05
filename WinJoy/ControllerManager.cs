using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinJoy
{
    static internal class ControllerManager
    {
        static Dictionary<string, ControllerDevice> Devices = new();

        static internal void AddController(string bluetoothId, string containerId)
        {
            Devices.Add(bluetoothId, new ControllerDevice(bluetoothId, containerId));
        }

        static internal void RemoveController(string bluetoothId)
        {
            if (Devices.ContainsKey(bluetoothId) == false) return;

            var device = Devices[bluetoothId];
            device.Dispose();
            Devices.Remove(bluetoothId);
        }
    }
}
