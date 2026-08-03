using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    public class IOSMirrorProcessRegistry
    {
        private readonly ConcurrentDictionary<string, IOSMirrorSession> _sessions = new();
        private readonly ILogger<IOSMirrorProcessRegistry> _logger;

        public IOSMirrorProcessRegistry(ILogger<IOSMirrorProcessRegistry> logger)
        {
            _logger = logger;
        }

        public void Register(string udid, Process process, string mode, string executable, string arguments, int? port = null)
        {
            try
            {
                // Un proceso lanzado via Scheduled Task corre elevado (mas privilegios que esta API),
                // y Windows no deja abrir el handle de espera que EnableRaisingEvents necesita sobre un
                // proceso mas privilegiado que el que lo pide (Access Denied) - la lectura de Id/HasExited
                // sigue funcionando (solo necesitan PROCESS_QUERY_LIMITED_INFORMATION), asi que se sigue
                // pudiendo trackear el proceso, solo sin el auto-cleanup al salir.
                process.EnableRaisingEvents = true;
                process.Exited += (_, _) =>
                {
                    _logger.LogInformation("[iOS Mirror] Proceso cerrado para {Udid} (PID={Pid})", udid, process.Id);
                    _sessions.TryRemove(udid, out _);
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[iOS Mirror] No se pudo suscribir al evento Exited del proceso para {Udid} (probablemente corre con mas privilegios); se sigue trackeando sin auto-cleanup", udid);
            }

            _sessions[udid] = new IOSMirrorSession
            {
                Udid = udid,
                Process = process,
                Mode = mode,
                Executable = executable,
                Arguments = arguments,
                Port = port,
                StartedAtUtc = DateTime.UtcNow
            };
        }

        public IOSMirrorSession? GetAlive(string udid)
        {
            if (_sessions.TryGetValue(udid, out var session) && !session.Process.HasExited)
                return session;

            return null;
        }

        public bool IsActive(string udid)
            => GetAlive(udid) != null;

        public IOSMirrorSession? Remove(string udid)
        {
            _sessions.TryRemove(udid, out var session);
            return session;
        }

        public List<IOSMirrorSessionResponse> GetActiveSessions()
        {
            return _sessions.Values
                .Where(s => !s.Process.HasExited)
                .Select(s => s.ToResponse())
                .ToList();
        }
    }

    public sealed class IOSMirrorSession
    {
        public string Udid { get; set; }
        public string Mode { get; set; }
        public string Executable { get; set; }
        public string Arguments { get; set; }
        public int? Port { get; set; }
        public Process Process { get; set; }
        public DateTime StartedAtUtc { get; set; }

        public IOSMirrorSessionResponse ToResponse()
            => new()
            {
                Udid = Udid,
                Mode = Mode,
                Executable = Executable,
                Arguments = Arguments,
                ProcessId = Process.Id,
                Port = Port,
                StartedAtUtc = StartedAtUtc
            };
    }
}
