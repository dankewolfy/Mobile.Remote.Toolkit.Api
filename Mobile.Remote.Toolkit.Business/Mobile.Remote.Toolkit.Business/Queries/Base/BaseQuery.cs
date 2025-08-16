using MediatR;

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Queries.Base
{
    /// <summary>
    /// Clase base para queries sin respuesta específica
    /// </summary>
    [DataContract]
    public abstract class BaseQuery : IRequest { }

    /// <summary>
    /// Clase base genérica para queries con respuesta específica
    /// </summary>
    /// <typeparam name="TResponse">Tipo de respuesta que devuelve la query</typeparam>
    [DataContract]
    public abstract class BaseQuery<TResponse> : IRequest<TResponse> { }
}
