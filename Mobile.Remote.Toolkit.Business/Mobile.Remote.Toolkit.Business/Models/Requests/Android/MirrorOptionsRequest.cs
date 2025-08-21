namespace Mobile.Remote.Toolkit.Business.Models.Requests.Android
{
    public record MirrorOptionsRequest
    {
        public bool StayAwake { get; init; } = true;
        public bool NoAudio { get; init; } = false;
        public bool ShowTouches { get; init; } = false;
        public bool TurnScreenOff { get; init; } = false;
        public int MaxSize { get; init; } = 1920;
        public string BitRate { get; init; } = "8M";
        public int MaxFps { get; init; } = 30;
    }
}
