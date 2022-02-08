using System;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    public abstract class ModelBase
    {
        /// <summary>
        /// The UTC timestamp when this object was created.
        /// </summary>
        /// <example>2008-01-14T04:33:35Z</example>
        public DateTime createdAt { get; internal set; }

        /// <summary>
        /// The last UTC timestamp when the this object was updated.
        /// </summary>
        /// <example>2008-01-14T04:33:35Z</example>
        public DateTime updatedAt { get; internal set; }
    }
}
