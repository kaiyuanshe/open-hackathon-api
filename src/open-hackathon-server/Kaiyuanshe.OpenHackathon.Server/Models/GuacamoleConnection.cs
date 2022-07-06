namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// info for remote connection like vnc, ssh and rdp.
    /// </summary>
    public abstract class GuacamoleConnection
    {
        public string name { get; set; }

        /// <summary>
        /// protocol for remote connection
        /// </summary>
        /// <example>vnc</example>
        public IngressProtocol protocol { get; internal set; }
    }

    public class GuacamoleConnectionList : ResourceList<object>
    {
        /// <summary>
        /// a list of connection info.
        /// </summary>
        public override object[] value { get; set; }
    }

    /// <summary>
    /// Vnc Connection settings for Guacamole.
    /// See also: https://guacamole.apache.org/doc/gug/configuring-guacamole.html#vnc
    /// </summary>
    public class VncConnection : GuacamoleConnection
    {
        /// <summary>
        /// The hostname or IP address of the VNC server Guacamole should connect to.
        /// </summary>
        public string? hostname { get; internal set; }

        /// <summary>
        /// The port the VNC server is listening on, usually 5900 or 5900 + display number. For example, if your VNC server is serving display number 1 (sometimes written as :1), your port number here would be 5901.
        /// </summary>
        public int port { get; internal set; }

        /// <summary>
        /// The username to use when attempting authentication, if any. This parameter is optional.
        /// </summary>
        public string? username { get; internal set; }

        /// <summary>
        /// The password to use when attempting authentication, if any. This parameter is optional.
        /// </summary>
        public string? password { get; internal set; }
    }
}
