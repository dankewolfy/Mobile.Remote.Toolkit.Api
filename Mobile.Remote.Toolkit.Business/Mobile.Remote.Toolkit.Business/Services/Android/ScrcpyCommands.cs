using Mobile.Remote.Toolkit.Business.Models.Requests.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public static class ScrcpyCommands
    {
        public static string StartMirror(string serial, Dictionary<string, object> options)
        {
            var args = $"-s {serial}";
            if (options != null)
            {
                if (options.TryGetValue("MaxSize", out var maxSize)) args += $" --max-size={maxSize}";
                if (options.TryGetValue("BitRate", out var bitRate)) args += $" --bit-rate={bitRate}";
                if (options.TryGetValue("MaxFps", out var maxFps)) args += $" --max-fps={maxFps}";
                if (options.TryGetValue("StayAwake", out var stayAwake) && stayAwake is bool b1 && b1) args += " --stay-awake";
                if (options.TryGetValue("NoAudio", out var noAudio) && noAudio is bool b2 && b2) args += " --no-audio";
                if (options.TryGetValue("ShowTouches", out var showTouches) && showTouches is bool b3 && b3) args += " --show-touches";
                if (options.TryGetValue("TurnScreenOff", out var turnScreenOff) && turnScreenOff is bool b4 && b4) args += " --turn-screen-off";
            }
            return args;
        }
    }
}
