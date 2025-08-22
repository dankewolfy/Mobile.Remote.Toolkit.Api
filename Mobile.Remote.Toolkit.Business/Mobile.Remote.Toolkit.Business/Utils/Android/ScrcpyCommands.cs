namespace Mobile.Remote.Toolkit.Business.Utils.Android
{
    public static class ScrcpyCommands
    {
        /// <summary>
        /// <summary>
        /// Genera los argumentos de línea de comandos para iniciar la duplicación de pantalla con scrcpy.
        /// Ejemplo de opciones:
        /// {
        ///   "MaxSize": 1024,
        ///   "BitRate": "8M",
        ///   "MaxFps": 60,
        ///   "StayAwake": true,
        ///   "NoAudio": true,
        ///   "ShowTouches": true,
        ///   "TurnScreenOff": true
        /// }
        /// </summary>
        /// <param name="serial">Número de serie del dispositivo Android.</param>
        /// <param name="options">Diccionario de opciones para configurar scrcpy.</param>
        /// <returns>Cadena de argumentos para scrcpy.</returns>
        ///{
        ///  "options": {
        ///    "MaxSize": 1024,
        ///    "BitRate": "8M",
        ///    "MaxFps": 60,
        ///    "StayAwake": true,
        ///    "NoAudio": true,
        ///    "ShowTouches": true,
        ///    "TurnScreenOff": true
        ///  }
        ///}
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string StartMirror(string serial, Dictionary<string, object> options)
        {
            var args = $"-s {serial}";
            if (options != null)
            {
                if (options.TryGetValue("MaxSize", out var maxSize)) args += $" --max-size={maxSize}";
                if (options.TryGetValue("BitRate", out var bitRate)) args += $" --video-bit-rate={bitRate}";
                if (options.TryGetValue("MaxFps", out var maxFps)) args += $" --max-fps={maxFps}";
                if (options.TryGetValue("StayAwake", out var stayAwake) && IsTrue(stayAwake)) args += " --stay-awake";
                if (options.TryGetValue("NoAudio", out var noAudio) && IsTrue(noAudio)) args += " --no-audio";
                if (options.TryGetValue("ShowTouches", out var showTouches) && IsTrue(showTouches)) args += " --show-touches";
                if (options.TryGetValue("TurnScreenOff", out var turnScreenOff) && IsTrue(turnScreenOff)) args += " --turn-screen-off";
            }
            return args;
        }

        private static bool IsTrue(object value)
        {
            if (value is bool b)
                return b;
            if (value is string s && bool.TryParse(s, out var result))
                return result;
            if (value is System.Text.Json.JsonElement je)
                return je.ValueKind == System.Text.Json.JsonValueKind.True
                    || (je.ValueKind == System.Text.Json.JsonValueKind.String && bool.TryParse(je.GetString(), out var r) && r);
            return false;
        }
    }
}
