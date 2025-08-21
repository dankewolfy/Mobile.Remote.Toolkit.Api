namespace Mobile.Remote.Toolkit.Business.Models.Android
{
    public record AndroidDeviceResponse
    {
        public string Id { get; init; }
        public string Serial { get; init; }
        public string Name { get; init; }
        public string Brand { get; init; }
        public string Model { get; init; }
        public string AndroidVersion { get; init; }
        public string Platform { get; init; }
    }
}
