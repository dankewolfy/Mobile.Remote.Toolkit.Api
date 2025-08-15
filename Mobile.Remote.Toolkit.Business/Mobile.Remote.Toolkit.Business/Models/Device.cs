using System.ComponentModel.DataAnnotations;

namespace Mobile.Remote.Toolkit.Business.Models
{
    public sealed class Device
    {
        /// <summary>
        /// 
        /// </summary>
        [Required]
        public required string Serial { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required]
        public required string Platform { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? FirstSeen { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Active { get; set; }
    }
}
