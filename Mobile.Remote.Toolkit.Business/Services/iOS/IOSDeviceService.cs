using System.Collections.Generic;
using System.Threading.Tasks;
using iMobileDevice;
using Mobile.Remote.Toolkit.Business.Models.Responses.iOS;

namespace Mobile.Remote.Toolkit.Business.Services.iOS
{
    public class IOSDeviceService
    {
        public async Task<List<IOSDeviceResponse>> GetDevicesAsync()
        {
            var devices = new List<IOSDeviceResponse>();
            var deviceList = new List<string>();

            // Enumerar dispositivos conectados
            using (var idevice = new iMobileDevice.iDevice())
            {
                idevice.GetDeviceList(out deviceList);
                foreach (var udid in deviceList)
                {
                    var device = new IOSDeviceResponse
                    {
                        Udid = udid,
                        Name = "iOS Device", // Se puede obtener con más detalle si se requiere
                        Model = "Unknown",
                        ProductType = "Unknown",
                        ProductVersion = "Unknown",
                        IsOnline = true
                    };
                    devices.Add(device);
                }
            }
            return devices;
        }
    }
}
