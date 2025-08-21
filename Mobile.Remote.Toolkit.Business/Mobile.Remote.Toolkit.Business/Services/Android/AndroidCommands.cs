namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public static class AndroidCommands
    {
        public static class Properties
        {
            public const string Brand = "ro.product.brand";
            public const string Model = "ro.product.model";
            public const string Version = "ro.build.version.release";
        }

        public static string GetProperty(string serial, string property)
            => $"-s {serial} shell getprop {property}";

        public const string ListDevices = "devices";
    }
}
