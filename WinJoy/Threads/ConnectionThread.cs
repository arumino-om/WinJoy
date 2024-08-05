using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;
using System.Diagnostics;

namespace WinJoy.Threads
{
    internal class ConnectionThread
    {
        private DeviceWatcher deviceWatcher;
        private List<string> targetDeviceNames = new() { "Joy-Con (L)", "Joy-Con (R)", "Pro Controller" };

        internal ConnectionThread()
        {
            string aqsFilter = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"";

            deviceWatcher = DeviceInformation.CreateWatcher(
                aqsFilter,
                new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.Bluetooth.Le.IsConnectable" },
                DeviceInformationKind.AssociationEndpoint);

            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            deviceWatcher.Start();
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var bluetoothDevice = await BluetoothDevice.FromIdAsync(args.Id);
            
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            var bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
            // Nintendo Switch 関連のコントローラーである場合
            if ((bluetoothDevice.ClassOfDevice.RawValue & 0xFFF) == 0x508 && targetDeviceNames.Contains(bluetoothDevice.Name))
            {
                // ペアリングされているが接続されていない場合，一旦ペアリングを解除する
                if (deviceInfo.Pairing.IsPaired && bluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                {
                    ControllerManager.RemoveController(deviceInfo.Id);
                    await deviceInfo.Pairing.UnpairAsync();
                }
                
                // ペアリングを行う
                DeviceInformationCustomPairing p = deviceInfo.Pairing.Custom;
                p.PairingRequested += (sender, args) =>
                {
                    if (args.PairingKind == DevicePairingKinds.ConfirmOnly) args.Accept();
                };
                var pairResult = await p.PairAsync(DevicePairingKinds.ConfirmOnly);
                if (!(pairResult.Status == DevicePairingResultStatus.Paired || pairResult.Status == DevicePairingResultStatus.AlreadyPaired))
                {
                    // ペアリングに失敗した場合
                    Debug.WriteLine($"Failed to pair with {bluetoothDevice.Name}");
                    return;
                }
                Debug.WriteLine($"Paired with {bluetoothDevice.Name}");
                
                // コントローラーマネージャーにデバイスを追加
                ControllerManager.AddController(deviceInfo.Id, deviceInfo.Properties["System.Devices.Aep.DeviceAddress"].ToString().Replace(":", null));
            }
            // UIに追加するコードをここに書く
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {

        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            // ウォッチャーが停止したときの処理
        }

        private static void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            switch (args.PairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                    // Windows itself will pop the confirmation dialog as part of "consent" if this is running on Desktop or Mobile
                    // If this is an App for 'Windows IoT Core' or a Desktop and Console application
                    // where there is no Windows Consent UX, you may want to provide your own confirmation.
                    args.Accept();
                    break;
            }
        }
    }
}
