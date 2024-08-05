using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using WinJoy.Types;

namespace WinJoy
{
    internal static class ControllerManager
    {
        private static Dictionary<string, ControllerDevice> _physicalControllers = new();
        private static List<IXbox360Controller> _virtualControllers = new();

        /// <summary>
        /// 物理コントローラーと仮想コントローラーのマッピング
        /// </summary>
        private static Dictionary<string, int> _deviceMapping = new();

        private static ViGEmClient _vigemClient = new();

        internal static void AddController(string bluetoothId, string serialNumber)
        {
            var device = new ControllerDevice(bluetoothId, serialNumber);
            device.SetOnControllerInputReceived(OnControllerInputReceived);
            _physicalControllers.Add(serialNumber, device);

            switch (device.ControllerType)
            {
                // Proコントローラーの場合
                case ControllerType.ProController:
                {
                    var virtualController = _vigemClient.CreateXbox360Controller();
                    _virtualControllers.Add(virtualController);
                    _deviceMapping.Add(serialNumber, _virtualControllers.Count - 1);
                    break;
                }
                // 左Joy-Conの場合
                case ControllerType.JoyConLeft:
                {
                    // 右Joy-Conのみ登録されている仮想コントローラーのインデックスを取得する
                    var virtualControllerIndex = _physicalControllers
                        .Where(controller => controller.Value.ControllerType == ControllerType.JoyConRight)
                        .Select(controller => _deviceMapping.GetValueOrDefault(controller.Key, -1))
                        .FirstOrDefault(index => 
                            index != -1 && _deviceMapping.Count(mapping => mapping.Value == index) == 1);

                    if (virtualControllerIndex == -1)
                    {
                        // 右Joy-Conのみ登録されている仮想コントローラーがない場合は、新しい仮想コントローラーを作成する
                        var vCon = _vigemClient.CreateXbox360Controller();
                        _virtualControllers.Add(vCon);
                        virtualControllerIndex = _virtualControllers.Count - 1;
                        vCon.Connect();
                    }
                
                    _deviceMapping.Add(serialNumber, virtualControllerIndex);
                    break;
                }
                // 右Joy-Conの場合
                case ControllerType.JoyConRight:
                {
                    // 左Joy-Conのみ登録されている仮想コントローラーのインデックスを取得する
                    var virtualControllerIndex = _physicalControllers
                        .Where(controller => controller.Value.ControllerType == ControllerType.JoyConLeft)
                        .Select(controller => _deviceMapping.GetValueOrDefault(controller.Key, -1))
                        .FirstOrDefault(index => 
                            index != -1 && _deviceMapping.Count(mapping => mapping.Value == index) == 1);

                    if (virtualControllerIndex == -1)
                    {
                        // 左Joy-Conのみ登録されている仮想コントローラーがない場合は、新しい仮想コントローラーを作成する
                        var vCon = _vigemClient.CreateXbox360Controller();
                        _virtualControllers.Add(vCon);
                        virtualControllerIndex = _virtualControllers.Count - 1;
                        vCon.Connect();
                    }
                
                    _deviceMapping.Add(serialNumber, virtualControllerIndex);
                    break;
                }
            }
        }

        internal static void RemoveController(string bluetoothId)
        {
            if (!_physicalControllers.ContainsKey(bluetoothId)) return;

            var device = _physicalControllers[bluetoothId];
            device.Dispose();
            _physicalControllers.Remove(bluetoothId);
        }

        private static void OnControllerInputReceived(string serialNumber, byte[] data)
        {
            if (!_physicalControllers.ContainsKey(serialNumber)) return;

            
        }
    }
}
