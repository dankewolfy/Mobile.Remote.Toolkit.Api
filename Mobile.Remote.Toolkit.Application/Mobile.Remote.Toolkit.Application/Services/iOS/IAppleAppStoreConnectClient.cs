namespace Mobile.Remote.Toolkit.Application.Services.iOS
{
    public interface IAppleAppStoreConnectClient
    {
        Task<bool> IsDeviceRegisteredAsync(string udid);
        Task RegisterDeviceAsync(string udid, string name);

        /// <summary>
        /// Regenera el provisioning profile de Development (necesario porque DeviceKit corre como
        /// XCUITest y requiere el entitlement get-task-allow) incluyendo todos los dispositivos
        /// habilitados en la cuenta (Apple no permite "agregar" un dispositivo a un profile
        /// existente, solo emitir uno nuevo con el conjunto completo). Devuelve el contenido crudo
        /// del .mobileprovision.
        /// </summary>
        Task<byte[]> RegenerateDeviceKitProfileAsync();
    }
}
