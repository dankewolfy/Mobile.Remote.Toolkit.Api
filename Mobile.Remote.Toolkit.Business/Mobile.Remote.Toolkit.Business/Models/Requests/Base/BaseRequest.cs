#nullable disable

using MediatR;

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Base
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public abstract class BaseRequest : IRequest { }
}
