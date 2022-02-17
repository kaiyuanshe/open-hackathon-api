using Kaiyuanshe.OpenHackathon.Server.Models;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Template for hackathon experiment.
    /// PK: hackathon
    /// RK: template id
    /// </summary>
    public class TemplateEntity : BaseTableEntity
    {
        /// <summary>
        /// name of Hackathon. PartitionKey
        /// </summary>
        [IgnoreEntityProperty]
        public string HackathonName
        {
            get
            {
                return PartitionKey;
            }
        }

        /// <summary>
        /// id of template. RowKey. Auto-generated Guid.
        /// </summary>
        [IgnoreEntityProperty]
        public string Id
        {
            get
            {
                return RowKey;
            }
        }

        public string DisplayName { get; set; }

        /// <summary>
        /// Container image.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// commands to start a container.
        /// </summary>
        [ConvertableEntityProperty]
        public string[] Commands { get; set; }

        /// <summary>
        /// environment variables passed to the container.
        /// </summary>
        [ConvertableEntityProperty]
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// protocol for remote connection
        /// </summary>
        /// <example>vnc</example>
        public IngressProtocol IngressProtocol { get; set; }

        /// <summary>
        /// Port for remote connection
        /// </summary>
        /// <example>5901</example>
        public int IngressPort { get; set; }

        /// <summary>
        /// vnc settings.
        /// </summary>
        [ConvertableEntityProperty]
        public VncSettings Vnc { get; set; }
    }
}
