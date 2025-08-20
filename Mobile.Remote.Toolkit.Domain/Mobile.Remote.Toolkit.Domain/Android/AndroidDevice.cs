#nullable disable

using System.ComponentModel.DataAnnotations;

namespace Mobile.Remote.Toolkit.Domain.Android
{
    public class AndroidDevice
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public string Serial { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Brand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string AndroidVersion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime FirstSeen { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime LastSeen { get; set; }
    }
}
