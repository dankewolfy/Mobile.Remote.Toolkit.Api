using MediatR;
using System.Collections.Generic;
using Mobile.Remote.Toolkit.Business.Models.Responses.iOS;

namespace Mobile.Remote.Toolkit.Business.Queries.iOS
{
    public class GetIOSDevicesQuery : IRequest<List<IOSDeviceResponse>>
    {
    }
}
