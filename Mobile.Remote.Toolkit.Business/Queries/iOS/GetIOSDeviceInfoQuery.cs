using MediatR;

namespace Mobile.Remote.Toolkit.Business.Queries.iOS
{
    public class GetIOSDeviceInfoQuery : IRequest<Models.Responses.iOS.IOSDeviceResponse>
    {
        public string Udid { get; }
        public GetIOSDeviceInfoQuery(string udid)
        {
            Udid = udid;
        }
    }
}
