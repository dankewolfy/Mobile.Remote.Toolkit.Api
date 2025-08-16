#nullable disable

using MediatR;

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Base
{

    /// <summary>
    /// Clase base para requests sin respuesta específica
    /// </summary>
    [DataContract]
    public abstract record BaseRequest : IRequest { }

    /// <summary>
    /// Clase base genérica para requests con respuesta específica
    /// </summary>
    /// <typeparam name="TResponse">Tipo de respuesta que devuelve el request</typeparam>
    [DataContract]
    public abstract record BaseRequest<TResponse> : IRequest<TResponse> { }
}
