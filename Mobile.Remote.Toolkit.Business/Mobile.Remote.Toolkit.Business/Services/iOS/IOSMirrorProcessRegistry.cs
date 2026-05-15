using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Models.Responses.iOS;

namespace Mobile.Remote.Toolkit.Business.Services.iOS
{
    public class IOSMirrorProcessRegistry
    {
        private readonly ConcurrentDictionary<string, IOSMirrorSession> _sessions = new();
        private readonly ILogger<IOSMirrorProcessRegistry> _logger;

        public IOSMirrorProcessRegistry(ILogger<IOSMirrorProcessRegistry> logger)
        {
            _logger = logger;
        }

        public void Register(string udid, Process process, string mode, string executable, string arguments)
        {
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                _logger.LogInformation("[iOS Mirror] Proceso cerrado para {Udid} (PID={Pid})", udid, process.Id);
                _sessions.TryRemove(udid, out _);
            };

            _sessions[udid] = new IOSMirrorSession
            {
                Udid = udid,
                Process = process,
                Mode = mode,
                Executable = executable,
                Arguments = arguments,
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
                StartedAtUtc = StartedAtUtc
            };
    }
}
