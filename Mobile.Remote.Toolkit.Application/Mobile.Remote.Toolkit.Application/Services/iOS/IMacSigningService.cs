namespace Mobile.Remote.Toolkit.Application.Services.iOS
{
    public interface IMacSigningService
    {
        /// <summary>
        /// Sube el .ipa base y el provisioning profile a un Mac remoto ya preparado (Xcode +
        /// certificado de firma instalados), dispara el script de re-firmado por SSH y devuelve
        /// el .ipa ya firmado. El certificado/clave privada de firma nunca sale del Mac.
        /// </summary>
        Task<byte[]> ResignIpaAsync(byte[] baseIpa, byte[] provisioningProfile);
    }
}
