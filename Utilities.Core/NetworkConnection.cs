using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace Utilities.core;

public class NetworkConnection : IDisposable
{
    private readonly string _networkName;

    public NetworkConnection(string networkName, NetworkCredential credentials)
    {
        _networkName = networkName;

        var netResource = new NetResource
        {
            Scope = ResourceScope.GlobalNetwork,
            ResourceType = ResourceType.Disk,
            DisplayType = ResourceDisplaytype.Share,
            RemoteName = networkName.TrimEnd('\\')
        };

        var result = WNetAddConnection2(
            netResource, credentials.Password, credentials.UserName, 0);

        if (result != 0)
        {
            throw new Win32Exception(result);
        }
    }

    public event EventHandler<EventArgs> Disposed;


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            var handler = Disposed;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        WNetCancelConnection2(_networkName, 0, true);
    }

    /// <summary>
    ///The WNetAddConnection2 function makes a connection to a network resource. The function can redirect a local device to the network resource.
    /// </summary>
    /// <param name="netResource">A <see cref="NetResource"/> structure that specifies details of the proposed connection, such as information about the network resource, the local device, and the network resource provider.</param>
    /// <param name="password">The password to use when connecting to the network resource.</param>
    /// <param name="username">The username to use when connecting to the network resource.</param>
    /// <param name="flags">The flags. See http://msdn.microsoft.com/en-us/library/aa385413%28VS.85%29.aspx for more information.</param>
    /// <returns></returns>
    [DllImport("mpr.dll")]
    private static extern int WNetAddConnection2(NetResource netResource,
                                                 string password,
                                                 string username,
                                                 int flags);

    /// <summary>
    /// The WNetCancelConnection2 function cancels an existing network connection. You can also call the function to remove remembered network connections that are not currently connected.
    /// </summary>
    /// <param name="name">Specifies the name of either the redirected local device or the remote network resource to disconnect from.</param>
    /// <param name="flags">Connection type. The following values are defined:
    /// 0: The system does not update information about the connection. If the connection was marked as persistent in the registry, the system continues to restore the connection at the next logon. If the connection was not marked as persistent, the function ignores the setting of the CONNECT_UPDATE_PROFILE flag.
    /// CONNECT_UPDATE_PROFILE: The system updates the user profile with the information that the connection is no longer a persistent one. The system will not restore this connection during subsequent logon operations. (Disconnecting resources using remote names has no effect on persistent connections.)
    /// </param>
    /// <param name="force">Specifies whether the disconnection should occur if there are open files or jobs on the connection. If this parameter is FALSE, the function fails if there are open files or jobs.</param>
    /// <returns></returns>
    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);


    /// <summary>
    /// Finalizes an instance of the <see cref="NetworkConnection"/> class.
    /// Allows an <see cref="System.Object"></see> to attempt to free resources and perform other cleanup operations before the <see cref="System.Object"></see> is reclaimed by garbage collection.
    /// </summary>
    ~NetworkConnection()
    {
        Dispose(false);
    }
}

#region "Objects needed for the Win32 functions"
#pragma warning disable 1591

/// <summary>
/// The net resource.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class NetResource
{
    public ResourceScope Scope;
    public ResourceType ResourceType;
    public ResourceDisplaytype DisplayType;
    public int Usage;
    public string LocalName;
    public string RemoteName;
    public string Comment;
    public string Provider;
}

/// <summary>
/// The resource scope.
/// </summary>
public enum ResourceScope
{
    Connected = 1,
    GlobalNetwork,
    Remembered,
    Recent,
    Context
}

/// <summary>
/// The resource type.
/// </summary>
public enum ResourceType
{
    Any = 0,
    Disk = 1,
    Print = 2,
    Reserved = 8,
}

/// <summary>
/// The resource displaytype.
/// </summary>
public enum ResourceDisplaytype
{
    Generic = 0x0,
    Domain = 0x01,
    Server = 0x02,
    Share = 0x03,
    File = 0x04,
    Group = 0x05,
    Network = 0x06,
    Root = 0x07,
    Shareadmin = 0x08,
    Directory = 0x09,
    Tree = 0x0a,
    Ndscontainer = 0x0b
}

#pragma warning restore 1591
#endregion
