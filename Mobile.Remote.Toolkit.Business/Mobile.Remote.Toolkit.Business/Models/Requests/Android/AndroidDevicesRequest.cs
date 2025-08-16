using System.Runtime.Serialization;

using Mobile.Remote.Toolkit.Business.Models.Requests.Base;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Android
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public sealed record AndroidDevicesRequest : BaseRequest<List<AndroidDeviceResponse>> { }
}
