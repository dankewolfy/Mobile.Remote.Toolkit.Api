using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Mobile.Remote.Toolkit.Business.Models.Responses.iOS;

namespace Mobile.Remote.Toolkit.Business.Services.iOS
{
    public class IOSDeviceService
    {
        public async Task<List<IOSDeviceResponse>> GetDevicesAsync()
        {
            var devices = new List<IOSDeviceResponse>();

            // Ruta al ejecutable idevice_id.exe incluido por el paquete
            string exePath = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "idevice_id.exe");
            if (!System.IO.File.Exists(exePath))
                exePath = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x86", "native", "idevice_id.exe");

            if (System.IO.File.Exists(exePath))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "-l",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                var udids = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                // Ruta al ejecutable ideviceinfo.exe
                string infoExePath = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", "ideviceinfo.exe");
                if (!System.IO.File.Exists(infoExePath))
                    infoExePath = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x86", "native", "ideviceinfo.exe");

                foreach (var udid in udids)
                {
                    string name = "iOS Device";
                    string model = "Unknown";
                    string productType = "Unknown";
                    string productVersion = "Unknown";

                    if (System.IO.File.Exists(infoExePath))
                    {
                        var infoProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = infoExePath,
                                Arguments = $"-u {udid}",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        infoProcess.Start();
                        var infoOutput = await infoProcess.StandardOutput.ReadToEndAsync();
                        infoProcess.WaitForExit();

                        // Parsear la salida de ideviceinfo
                        var lines = infoOutput.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("DeviceName:"))
                                name = line.Substring("DeviceName:".Length).Trim();
                            else if (line.StartsWith("Model:"))
                                model = line.Substring("Model:".Length).Trim();
                            else if (line.StartsWith("DeviceClass:"))
                                model = string.IsNullOrWhiteSpace(model) ? line.Substring("DeviceClass:".Length).Trim() : model;
                            else if (line.StartsWith("HardwareModel:"))
                                model = string.IsNullOrWhiteSpace(model) ? line.Substring("HardwareModel:".Length).Trim() : model;
                            else if (line.StartsWith("ProductType:"))
                                productType = line.Substring("ProductType:".Length).Trim();
                            else if (line.StartsWith("ProductVersion:"))
                                productVersion = line.Substring("ProductVersion:".Length).Trim();
                        }
                        // Si el modelo sigue siendo desconocido, usar ProductType como modelo
                        if (string.IsNullOrWhiteSpace(model) || model == "Unknown")
                        {
                            model = productType;
                        }
                    }

                    var device = new IOSDeviceResponse
                    {
                        Udid = udid,
                        Name = name,
                        Model = model,
                        ProductType = productType,
                        ProductVersion = productVersion,
                        IsOnline = true
                    };
                    devices.Add(device);
                }
            }
            return devices;
        }
    }
}
