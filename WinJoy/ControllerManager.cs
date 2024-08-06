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
using WinJoy.Helpers;

namespace WinJoy
{
    internal static class ControllerManager
    {
        private static Dictionary<string, ControllerDevice> _physicalControllers = new();
        private static List<IXbox360Controller> _virtualControllers = new();
        private static Dictionary<string, ushort[][]> _calibrationDataCache = new();    // 0...Left, 1...Right

        private enum StickIndex
        {
            Left,
            Right
        }

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
            //Debug.WriteLine($"[{bluetoothId}] CommandID: {rawData[0]}");

            var virtualController = _virtualControllers[virtualControllerIndex];
            
            if (rawData[0] == 0x3F)
            {
                ParseSimpleHidModeReport(virtualController, controllerType, rawData);
            }
            else if (rawData[0] == 0x30)
            {
                ParseFullModeReport(bluetoothId, virtualController, controllerType, rawData);
            }
            else
            {
                Debug.WriteLine($"[{bluetoothId}]: Unknown report ID {rawData[0]}");
            }
        }

        private static ushort[] QueryCalibrationData(string bluetoothId, StickIndex index)
        {
            if (_calibrationDataCache.TryGetValue(bluetoothId, out var data))
            {
                return data[(int)index];
            }

            var device = _physicalControllers[bluetoothId];
            var calibrationData = index == StickIndex.Left ? device.LeftAnalogStickCalibrationData : device.RightAnalogStickCalibrationData;

            if (index == StickIndex.Right)
            {
                // キャリブレーションデータの並び順を左スティックのものと合わせる
                var tmp = new ushort[calibrationData.Length];
                Array.Copy(calibrationData, tmp, calibrationData.Length);
                calibrationData[0] = tmp[4];
                calibrationData[1] = tmp[5];
                calibrationData[2] = tmp[0];
                calibrationData[3] = tmp[1];
                calibrationData[4] = tmp[2];
                calibrationData[5] = tmp[3];
            }

            if (!_calibrationDataCache.ContainsKey(bluetoothId))
            {
                _calibrationDataCache.Add(bluetoothId, new ushort[2][]);
            }

            _calibrationDataCache[bluetoothId][(int)index] = calibrationData;
            return calibrationData;
        }

        private static float[] GetActualStickData(ushort[] calibrationData, short rawX, short rawY)
        {
            var stickData = new float[2];

            float calX = rawX - calibrationData[2];
            float calY = rawY - calibrationData[3];

            stickData[0] = calX / (calX > 0 ? calibrationData[0] : calibrationData[4]);
            stickData[1] = calY / (calY > 0 ? calibrationData[1] : calibrationData[5]);

            return stickData;
        }

        private static void ParseFullModeReport(string bluetoothId, IXbox360Controller virtualController, ControllerType controllerType, byte[] rawData)
        {
            var batteryLevel = rawData[2] >> 4;
            var btnStatus1 = rawData[3..6];
            var leftStickData = rawData[6..9];
            var rightStickData = rawData[9..12];

            virtualController.SetButtonState(Xbox360Button.Back, ByteHelper.IsBitSet(btnStatus1[1], 0x01));
            virtualController.SetButtonState(Xbox360Button.Start, ByteHelper.IsBitSet(btnStatus1[1], 0x02));
            virtualController.SetButtonState(Xbox360Button.RightThumb, ByteHelper.IsBitSet(btnStatus1[1], 0x04));
            virtualController.SetButtonState(Xbox360Button.LeftThumb, ByteHelper.IsBitSet(btnStatus1[1], 0x08));
            virtualController.SetButtonState(Xbox360Button.Guide, ByteHelper.IsBitSet(btnStatus1[1], 0x10));
            virtualController.SetButtonState(Xbox360Button.Guide, ByteHelper.IsBitSet(btnStatus1[1], 0x20));

            switch (controllerType)
            {
                case ControllerType.JoyConRight:
                    // ABXYボタン
                    virtualController.SetButtonState(Xbox360Button.Y, ByteHelper.IsBitSet(btnStatus1[0], 0x01));
                    virtualController.SetButtonState(Xbox360Button.X, ByteHelper.IsBitSet(btnStatus1[0], 0x02));
                    virtualController.SetButtonState(Xbox360Button.B, ByteHelper.IsBitSet(btnStatus1[0], 0x04));
                    virtualController.SetButtonState(Xbox360Button.A, ByteHelper.IsBitSet(btnStatus1[0], 0x08));

                    // R, ZRボタン
                    virtualController.SetButtonState(Xbox360Button.RightShoulder, ByteHelper.IsBitSet(btnStatus1[0], 0x40));
                    virtualController.SetSliderValue(Xbox360Slider.RightTrigger, ByteHelper.IsBitSet(btnStatus1[0], 0x80) ? (byte)0xFF : (byte)0x0);

                    // スティック
                    short rightStickHorizontal = (short)(rightStickData[0] | ((rightStickData[1] & 0xF) << 8));
                    short rightStickVertical = (short)((rightStickData[1] >> 4) | (rightStickData[2] << 4));
                    virtualController.SetAxisValue(Xbox360Axis.RightThumbX, rightStickHorizontal);
                    virtualController.SetAxisValue(Xbox360Axis.RightThumbY, rightStickVertical);
                    break;

                case ControllerType.JoyConLeft:
                    // 十字キー
                    virtualController.SetButtonState(Xbox360Button.Down, ByteHelper.IsBitSet(btnStatus1[2], 0x01));
                    virtualController.SetButtonState(Xbox360Button.Up, ByteHelper.IsBitSet(btnStatus1[2], 0x02));
                    virtualController.SetButtonState(Xbox360Button.Right, ByteHelper.IsBitSet(btnStatus1[2], 0x04));
                    virtualController.SetButtonState(Xbox360Button.Left, ByteHelper.IsBitSet(btnStatus1[2], 0x08));

                    // L, ZLボタン
                    virtualController.SetButtonState(Xbox360Button.LeftShoulder, ByteHelper.IsBitSet(btnStatus1[2], 0x40));
                    virtualController.SetSliderValue(Xbox360Slider.LeftTrigger, ByteHelper.IsBitSet(btnStatus1[2], 0x80) ? (byte)0xFF : (byte)0x0);

                    // スティック
                    short leftStickHorizontal = (short)(leftStickData[0] | ((leftStickData[1] & 0xF) << 8));
                    short leftStickVertical = (short)((leftStickData[1] >> 4) | (leftStickData[2] << 4));
                    virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, leftStickHorizontal);
                    virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, leftStickVertical);
                    break;
            }
        }

        private static void ParseSimpleHidModeReport(IXbox360Controller virtualController, ControllerType controllerType, byte[] rawData)
        {
            var btnStatus1 = rawData[1];
            var btnStatus2 = rawData[2];

            switch (controllerType)
            {
                case ControllerType.JoyConLeft:
                    // 十字キー
                    virtualController.SetButtonState(Xbox360Button.Left, ByteHelper.IsBitSet(btnStatus1, 0x01));
                    virtualController.SetButtonState(Xbox360Button.Down, ByteHelper.IsBitSet(btnStatus1, 0x02));
                    virtualController.SetButtonState(Xbox360Button.Up, ByteHelper.IsBitSet(btnStatus1, 0x04));
                    virtualController.SetButtonState(Xbox360Button.Right, ByteHelper.IsBitSet(btnStatus1, 0x08));

                    // マイナスボタン
                    virtualController.SetButtonState(Xbox360Button.Back, ByteHelper.IsBitSet(btnStatus2, 0x01));
                    // Lスティックボタン
                    virtualController.SetButtonState(Xbox360Button.LeftThumb, ByteHelper.IsBitSet(btnStatus2, 0x04));
                    // キャプチャボタン
                    virtualController.SetButtonState(Xbox360Button.Guide, ByteHelper.IsBitSet(btnStatus2, 0x20));
                    // Lボタン
                    virtualController.SetButtonState(Xbox360Button.LeftShoulder, ByteHelper.IsBitSet(btnStatus2, 0x40));
                    // ZLボタン
                    virtualController.SetSliderValue(Xbox360Slider.LeftTrigger, ByteHelper.IsBitSet(btnStatus2, 0x80) ? (byte)0xFF : (byte)0x0);

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
                    virtualController.SetButtonState(Xbox360Button.A, ByteHelper.IsBitSet(btnStatus1, 0x01));
                    virtualController.SetButtonState(Xbox360Button.X, ByteHelper.IsBitSet(btnStatus1, 0x02));
                    virtualController.SetButtonState(Xbox360Button.B, ByteHelper.IsBitSet(btnStatus1, 0x04));
                    virtualController.SetButtonState(Xbox360Button.Y, ByteHelper.IsBitSet(btnStatus1, 0x08));

                    // プラスボタン
                    virtualController.SetButtonState(Xbox360Button.Start, ByteHelper.IsBitSet(btnStatus2, 0x02));
                    // Rスティックボタン
                    virtualController.SetButtonState(Xbox360Button.RightThumb, ByteHelper.IsBitSet(btnStatus2, 0x08));
                    // ホームボタン
                    virtualController.SetButtonState(Xbox360Button.Guide, ByteHelper.IsBitSet(btnStatus2, 0x10));
                    // Rボタン
                    virtualController.SetButtonState(Xbox360Button.RightShoulder, ByteHelper.IsBitSet(btnStatus2, 0x40));
                    // ZRボタン
                    virtualController.SetSliderValue(Xbox360Slider.RightTrigger, ByteHelper.IsBitSet(btnStatus2, 0x80) ? (byte)0xFF : (byte)0x0);

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
