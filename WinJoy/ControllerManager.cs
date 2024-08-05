using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
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
            _physicalControllers.Add(bluetoothId, device);

            lock(_vigemClient)
            {
                switch (device.ControllerType)
                {
                    // Proコントローラーの場合
                    case ControllerType.ProController:
                    {
                        var virtualController = _vigemClient.CreateXbox360Controller();
                        _virtualControllers.Add(virtualController);
                        _deviceMapping.Add(bluetoothId, _virtualControllers.Count - 1);
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
                                index != -1 && _deviceMapping.Count(mapping => mapping.Value == index) == 1, -1);

                        if (virtualControllerIndex == -1)
                        {
                            // 右Joy-Conのみ登録されている仮想コントローラーがない場合は、新しい仮想コントローラーを作成する
                            var vCon = _vigemClient.CreateXbox360Controller();
                            _virtualControllers.Add(vCon);
                            virtualControllerIndex = _virtualControllers.Count - 1;
                            vCon.Connect();
                        }
                    
                        _deviceMapping.Add(bluetoothId, virtualControllerIndex);
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
                                index != -1 && _deviceMapping.Count(mapping => mapping.Value == index) == 1, -1);

                        if (virtualControllerIndex == -1)
                        {
                            // 左Joy-Conのみ登録されている仮想コントローラーがない場合は、新しい仮想コントローラーを作成する
                            var vCon = _vigemClient.CreateXbox360Controller();
                            _virtualControllers.Add(vCon);
                            virtualControllerIndex = _virtualControllers.Count - 1;
                            vCon.Connect();
                        }
                    
                        _deviceMapping.Add(bluetoothId, virtualControllerIndex);
                        break;
                    }
                }
            }

            
        }

        internal static void RemoveController(string bluetoothId)
        {
            if (!_physicalControllers.TryGetValue(bluetoothId, out var device)) return;

            device.Dispose();
            _physicalControllers.Remove(bluetoothId);
            _deviceMapping.Remove(bluetoothId);
        }

        private static void OnControllerInputReceived(string bluetoothId, ControllerType controllerType, byte[] rawData)
        {
            if (!_physicalControllers.ContainsKey(bluetoothId)) return;
            if (!_deviceMapping.TryGetValue(bluetoothId, out var virtualControllerIndex)) return;
            Debug.WriteLine($"[{bluetoothId}] CommandID: {rawData[0]}");
            if (rawData[0] != 0x3F) return;

            var virtualController = _virtualControllers[virtualControllerIndex];
            
            if (rawData[0] == 0x3F)
            {
                ParseSimpleHidModeReport(virtualController, controllerType, rawData);
            }
            else if (rawData[0] == 0x30)
            {
                ParseFullModeReport(virtualController, controllerType, rawData);
            }
            else
            {
                Debug.WriteLine($"[{bluetoothId}]: Unknown report ID {rawData[0]}");
            }
        }

        private static void ParseFullModeReport(IXbox360Controller virtualController, ControllerType controllerType, byte[] rawData)
        {
            // 未実装
        }

        private static void ParseSimpleHidModeReport(IXbox360Controller virtualController, ControllerType controllerType, byte[] rawData)
        {
            var btnStatus1 = rawData[1];
            var btnStatus2 = rawData[2];

            Func<int, int, bool> isBitSet = (value, bit) => (value & bit) != 0;

            switch (controllerType)
            {
                case ControllerType.JoyConLeft:
                    // 十字キー
                    virtualController.SetButtonState(Xbox360Button.Left, isBitSet(btnStatus1, 0x01));
                    virtualController.SetButtonState(Xbox360Button.Down, isBitSet(btnStatus1, 0x02));
                    virtualController.SetButtonState(Xbox360Button.Up, isBitSet(btnStatus1, 0x04));
                    virtualController.SetButtonState(Xbox360Button.Right, isBitSet(btnStatus1, 0x08));

                    // マイナスボタン
                    virtualController.SetButtonState(Xbox360Button.Back, isBitSet(btnStatus2, 0x01));
                    // Lスティックボタン
                    virtualController.SetButtonState(Xbox360Button.LeftThumb, isBitSet(btnStatus2, 0x04));
                    // キャプチャボタン
                    virtualController.SetButtonState(Xbox360Button.Guide, isBitSet(btnStatus2, 0x20));
                    // Lボタン
                    virtualController.SetButtonState(Xbox360Button.LeftShoulder, isBitSet(btnStatus2, 0x40));
                    // ZLボタン
                    virtualController.SetSliderValue(Xbox360Slider.LeftTrigger, isBitSet(btnStatus2, 0x80) ? (byte)0xFF : (byte)0x0);

                    // スティックハット
                    switch (rawData[3])
                    {
                        case 0:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, 32767);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                            break;

                        case 1:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, 32767);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, -32767);
                            break;

                        case 2:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, -32767);
                            break;

                        case 3:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, -32767);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, -32767);
                            break;

                        case 4:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, -32767);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                            break;

                        case 5:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, -32767);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, 32767);
                            break;

                        case 6:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, 32767);
                            break;

                        case 7:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, 32767);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, 32767);
                            break;

                        case 8:
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                            break;
                    }
                    break;

                case ControllerType.JoyConRight:
                    // ABXYボタン
                    virtualController.SetButtonState(Xbox360Button.A, isBitSet(btnStatus1, 0x01));
                    virtualController.SetButtonState(Xbox360Button.X, isBitSet(btnStatus1, 0x02));
                    virtualController.SetButtonState(Xbox360Button.B, isBitSet(btnStatus1, 0x04));
                    virtualController.SetButtonState(Xbox360Button.Y, isBitSet(btnStatus1, 0x08));

                    // プラスボタン
                    virtualController.SetButtonState(Xbox360Button.Start, isBitSet(btnStatus2, 0x02));
                    // Rスティックボタン
                    virtualController.SetButtonState(Xbox360Button.RightThumb, isBitSet(btnStatus2, 0x08));
                    // ホームボタン
                    virtualController.SetButtonState(Xbox360Button.Guide, isBitSet(btnStatus2, 0x10));
                    // Rボタン
                    virtualController.SetButtonState(Xbox360Button.RightShoulder, isBitSet(btnStatus2, 0x40));
                    // ZRボタン
                    virtualController.SetSliderValue(Xbox360Slider.RightTrigger, isBitSet(btnStatus2, 0x80) ? (byte)0xFF : (byte)0x0);

                                        // スティックハット
                    switch (rawData[3])
                    {
                        case 0:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, -32767);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, 0);
                            break;

                        case 1:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, -32767);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, 32767);
                            break;

                        case 2:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, 32767);
                            break;

                        case 3:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, 32767);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, 32767);
                            break;

                        case 4:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, 32767);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, 0);
                            break;

                        case 5:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, 32767);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, -32767);
                            break;

                        case 6:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, -32767);
                            break;

                        case 7:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, -32767);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, -32767);
                            break;

                        case 8:
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, 0);
                            break;
                    }
                    break;
            }
        }
    }
}
