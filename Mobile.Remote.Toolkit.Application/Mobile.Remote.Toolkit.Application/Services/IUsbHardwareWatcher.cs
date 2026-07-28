namespace Mobile.Remote.Toolkit.Application.Services
{
    /// <summary>
    /// Puerto para detectar cambios de hardware USB (conexión/desconexión) que disparan un
    /// refresco de la lista de dispositivos. La implementación concreta decide el mecanismo
    /// (eventos nativos del SO, polling, etc.) — el consumidor solo conoce el callback.
    /// </summary>
    public interface IUsbHardwareWatcher : IDisposable
    {
        void Start(Func<Task> onHardwareChanged);
        void Stop();
    }
}
