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

namespace WinJoy
{
    internal class ControllerDevice : IDisposable
    {
        string _serialNumber;
        BluetoothDevice _bluetooth;
        Device _hid;

        byte _sendCount = 0;

        public ControllerType ControllerType { get; private set; }
        Action<string, ControllerType, byte[]> _onControllerInputReceived;

        internal ControllerDevice(string bluetoothId, string serialNumber)
        {
            _serialNumber = serialNumber;
            _bluetooth = BluetoothDevice.FromIdAsync(bluetoothId).GetAwaiter().GetResult();
            _bluetooth.ConnectionStatusChanged += ControllerDevice_ConnectionStatusChanged;
            if (_bluetooth.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                Hid.Init();
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
            }

            Task.Run(() =>
            {
                while (true)
                {
                    var data = _hid.Read(64);
                    if (data.Length == 0)
                    {
                        continue;
                    }

                    _onControllerInputReceived?.Invoke(_bluetooth.DeviceId, ControllerType, data.ToArray());
                }
            });
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

            _sendCount++;
            if (_sendCount > 0x0F)
            {
                _sendCount = 0;
            }

            List<byte> payload = [commandId, _sendCount, .. rumbleData];
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

        internal ReadOnlySpan<byte> ReadBytesFromHid(int readBytes)
        {
            return _hid.Read(readBytes);
        }

        private bool CreateHidDevice()
        {
            int retryCount = 0;
            while (retryCount < 2)
            {
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
            if (_bluetooth.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                CreateHidDevice();
            }
            else
            {
                _hid.Dispose();
            }
        }

        public void Dispose()
        {
            
        }
    }
}
