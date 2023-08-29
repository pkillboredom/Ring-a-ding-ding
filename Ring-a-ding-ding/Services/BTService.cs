using System;
using System.Linq;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;

namespace Ring_a_ding_ding.Services
{
    public class BTService
    {
        public BTService()
        {
        }

        private static Guid headsetUUID = new Guid("00001108-0000-1000-8000-00805f9b34fb");
        private static Guid handsfreeUUID = new Guid("0000111e-0000-1000-8000-00805f9b34fb");

        /// <summary>
        /// Returns the IDs and names of all connected Bluetooth devices.
        /// </summary>
        /// <returns>IDs and names of all connected Bluetooth Devices</returns>
        public async Task<Dictionary<string,string>> EnumerateConnectedBluetoothDevicesAsync()
        {
            var devices = BluetoothDevice.GetDeviceSelector();
            var deviceInformationCollection = await DeviceInformation.FindAllAsync(devices);
            Dictionary<string, string> DeviceList = new Dictionary<string, string>();

            foreach (var deviceInformation in deviceInformationCollection)
            {
                var btDevice = await BluetoothDevice.FromIdAsync(deviceInformation.Id);
                if (btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                    DeviceList.Add(btDevice.DeviceId, btDevice.Name);                    
            }
            return DeviceList;
        }

        public async Task<BTDeviceInfo?> GetDeviceInfoAsync(string deviceId)
        {
            try
            {
                var btDevice = await BluetoothDevice.FromIdAsync(deviceId);
                return new BTDeviceInfo()
                {
                    DeviceId = btDevice.DeviceId,
                    DeviceName = btDevice.Name,
                    ConnectionStatus = btDevice.ConnectionStatus,
                    DeviceClass = btDevice.ClassOfDevice
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task TryRingingBTDevice(string deviceId)
        {
            var btDevice = await BluetoothDevice.FromIdAsync(deviceId);
            var services = await btDevice.GetRfcommServicesAsync();
            var audioService = services.Services.FirstOrDefault(x => x.ServiceId.Uuid.Equals(headsetUUID));
            if (audioService == null) { 
                audioService = services.Services.FirstOrDefault(x => x.ServiceId.Uuid.Equals(handsfreeUUID)); 
            }
            if (audioService != null)
            {
                using (StreamSocket streamSocket = new StreamSocket())
                {
                    await streamSocket.ConnectAsync(audioService.ConnectionHostName, audioService.ConnectionServiceName);
                    var outputStream = streamSocket.OutputStream.AsStreamForWrite();
                    using (var writer = new StreamWriter(outputStream))
                    {
                        await writer.WriteLineAsync("AT+CHUP"); // hang up any active call - we want a ringing signal 
                        await writer.FlushAsync();
                        await writer.WriteLineAsync("RING"); // ring!
                        await writer.FlushAsync();
                    }
                }
            }
            else
            {
                throw new ArgumentException("Device is not a headset or hands-free target.");
            }
        }
    }

    public class BTDeviceInfo
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public BluetoothConnectionStatus ConnectionStatus { get; set; }
        public BluetoothClassOfDevice DeviceClass { get; set; }

    }
}
