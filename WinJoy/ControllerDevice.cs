using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.UI.Notifications;
using HidApi;
using System.Runtime.InteropServices.WindowsRuntime;
using WinJoy.Data;
using WinJoy.Types;
using WinJoy.Helpers;

namespace WinJoy
{
    internal class ControllerDevice : IDisposable
    {
        internal ControllerType ControllerType { get; private set; }
        internal ushort[] LeftAnalogStickCalibrationData { get; private set; }
        internal ushort[] RightAnalogStickCalibrationData { get; private set; }

        string _serialNumber;
        BluetoothDevice _bluetooth;
        Device _hid;
        
        byte _sendCount = 0;
        
        Action<string, ControllerType, byte[]> _onControllerInputReceived;

        internal ControllerDevice(string bluetoothId, string serialNumber)
        {
            _serialNumber = serialNumber;
            _bluetooth = BluetoothDevice.FromIdAsync(bluetoothId).GetAwaiter().GetResult();
            _bluetooth.ConnectionStatusChanged += ControllerDevice_ConnectionStatusChanged;
            if (_bluetooth.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                CreateHidDevice();
                switch (_hid.GetDeviceInfo().ProductId)
                {
                    case 0x2006:
                        ControllerType = ControllerType.JoyConLeft;
                        break;
                    case 0x2007:
                        ControllerType = ControllerType.JoyConRight;
                        break;
                    case 0x2009:
                        ControllerType = ControllerType.ProController;
                        break;
                }
                Debug.WriteLine($"[{ControllerType}]: Connected to HID device");

                // 一旦 Simple HID mode に変更
                SendCommand(0x01, [0x00, 0x01, 0x40, 0x40, 0x00, 0x01, 0x40, 0x40], 0x03, [0x3F]);

                // 各スティックのファクトリーキャリブレーションデータを取得
                SendCommand(0x01, [0x00, 0x01, 0x40, 0x40, 0x00, 0x01, 0x40, 0x40], 0x10, [0x3D, 0x60, 0x00, 0x00, 0x09]);
                var data1 = ReadBytesFromHid(64, 0x10);
                LeftAnalogStickCalibrationData = ByteHelper.Decode3ByteGroup(data1[20..29].ToArray());
                SendCommand(0x01, [0x00, 0x01, 0x40, 0x40, 0x00, 0x01, 0x40, 0x40], 0x10, [0x46, 0x60, 0x00, 0x00, 0x09]);
                var data2 = ReadBytesFromHid(64, 0x10);
                RightAnalogStickCalibrationData = ByteHelper.Decode3ByteGroup(data2[20..29].ToArray());

                // Standard full mode に変更する
                SendCommand(0x01, [0x00, 0x01, 0x40, 0x40, 0x00, 0x01, 0x40, 0x40], 0x03, [0x30]);


                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            var data = _hid.Read(64);
                            if (data.Length == 0)
                            {
                                continue;
                            }

                            _onControllerInputReceived(_bluetooth.DeviceId, ControllerType, data.ToArray());
                        }
                        catch (HidException e)
                        {
                            Debug.WriteLine($"[{_serialNumber}]: OMG! HidException occured: {e.Message}");
                        }
                    }
                });
            }
        }

        internal void SetOnControllerInputReceived(Action<string, ControllerType, byte[]> onDataReceived)
        {
            _onControllerInputReceived = onDataReceived;
        }

        internal void SendCommand(byte commandId, IEnumerable<byte> rumbleData, byte subCommandId = 0, IEnumerable<byte>? subCommandData = null)
        {
            if (rumbleData.Count() != 8)
            {
                throw new ArgumentException("rumbleData must be 8 bytes length");
            }

            List<byte> payload = [commandId, (byte)(++_sendCount & 0xF), .. rumbleData];
            if (commandId != 0x10)
            {
                payload.Add(subCommandId);
                if (subCommandData != null)
                {
                    payload.AddRange(subCommandData);
                }
            }

            _hid.Write(payload.ToArray());
        }

        internal ReadOnlySpan<byte> ReadBytesFromHid(int readBytes, byte expectSubCommand = 0)
        {
            var retryCount = 0;
            ReadOnlySpan<byte> data = null;
            while (retryCount < 4)
            {
                data = _hid.Read(readBytes);
                if (expectSubCommand == 0) return data;

                if (data[14] == expectSubCommand) return data;
                retryCount++;
            }

            return data;
        }

        private bool CreateHidDevice()
        {
            int retryCount = 0;
            while (retryCount < 10)
            {
                Hid.Init();
                var enums = Hid.Enumerate();
                var targetHid = enums.FirstOrDefault(x => x.SerialNumber == _serialNumber);
                if (targetHid == null)
                {
                    Task.Delay(1000).Wait();
                    retryCount++;
                    continue;
                }
                _hid = targetHid.ConnectToDevice();
                return true;
            }
            return false;
        }

        private void ControllerDevice_ConnectionStatusChanged(BluetoothDevice sender, object args)
        {
            Debug.WriteLine($"[{_serialNumber}]: Connection status changed: {sender.ConnectionStatus}");
        }

        public void Dispose()
        {
            
        }
    }
}
