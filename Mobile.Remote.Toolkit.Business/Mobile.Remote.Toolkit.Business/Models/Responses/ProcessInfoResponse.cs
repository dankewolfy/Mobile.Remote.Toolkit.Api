#nullable disable

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Responses;

/// <summary>
/// 
/// </summary>
[DataContract]
public class ProcessInfoResponse
{
    /// <summary>
    /// 
    /// </summary>
    [DataMember]
    public int Pid { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [DataMember]
    public string Name { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [DataMember]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [DataMember]
    public string MainWindowTitle { get; set; }
}
