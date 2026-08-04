using MediatR;

using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Queries.iOS
{
    public sealed class GetIOSDriverStatusQuery : IRequest<IOSDriverStatusResponse>
    {
        public class GetIOSDriverStatusQueryHandler : IRequestHandler<GetIOSDriverStatusQuery, IOSDriverStatusResponse>
        {
            private readonly IIOSDriverService _driverService;

            public GetIOSDriverStatusQueryHandler(IIOSDriverService driverService)
            {
                _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            }

            public async Task<IOSDriverStatusResponse> Handle(GetIOSDriverStatusQuery request, CancellationToken cancellationToken)
                => await _driverService.GetStatusAsync();
        }
    }
}
