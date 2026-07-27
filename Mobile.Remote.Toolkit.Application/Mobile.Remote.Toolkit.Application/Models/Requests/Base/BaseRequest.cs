#nullable disable

using MediatR;

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Application.Models.Requests.Base
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public abstract class BaseRequest : IRequest { }
}
