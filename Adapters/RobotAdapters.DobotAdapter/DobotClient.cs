using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;


namespace RobotAdapters.DobotAdapter;

public abstract class DobotClient
{
    private Socket? _socketClient;
    private string _ipAddress = string.Empty;
    private int _port;

    public string IP
    {
        get
        {
            return _ipAddress;
        }
    }
    public int Port
    {
        get
        {
            return _port;
        }
    }

    public bool Connect(string ipAddress, int port)
    {
        if (_socketClient is not null && IsConnected())
        {
            if (ipAddress != _ipAddress || port != _port)
            {
                this.Disconnect();
            }
            else
            {
                return true;
            }
        }
        return this.ConnectDobotServer(ipAddress, port);
    }

    private bool ConnectDobotServer(string ipAddress, int port)
    {
        try
        {
            _ipAddress = ipAddress;
            _port = port;

            var parsedIpAddress = IPAddress.Parse(_ipAddress);
            var endPoint = new IPEndPoint(parsedIpAddress, _port);

            _socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socketClient.Connect(endPoint);
            _socketClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            uint dummy = 0;
            byte[] optVal = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(optVal, 0);
            BitConverter.GetBytes((uint)5000).CopyTo(optVal, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)500).CopyTo(optVal, Marshal.SizeOf(dummy) * 2);
            _socketClient.IOControl(IOControlCode.KeepAliveValues, optVal, null);

            OnConnected(_socketClient);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connect failed: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        try
        {
            _socketClient?.Shutdown(SocketShutdown.Both);
            _socketClient?.Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("close socket:" + ex.ToString());
        }
    }

    public bool IsConnected()
    {
        try
        {
            if ((_socketClient is null || !_socketClient.Connected) && _port != 29999)
            {
                int port = _port;
            }
            if (_port == 29999 )
            {
                if (_socketClient is null || !_socketClient.Connected)
                {
                    this.ConnectDobotServer(_ipAddress, _port);
                    return false;
                }
            }
          
            return _socketClient is not null && _socketClient.Connected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
        return false;
    }

    protected abstract void OnConnected(Socket sock);
    protected abstract void OnDisconnected();

    public delegate void OnNetworkError(DobotClient sender, SocketError iErrCode);
    public event OnNetworkError? NetworkErrorEvent;

    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="str">发送内容</param>
    /// <returns>成功-true，失败-false</returns>
    protected bool SendData(string str)
    {
        try
        {
           /////////////////////
           System.Diagnostics.Debug.WriteLine("DOBOT_out: " + str);


            byte[] data = Encoding.UTF8.GetBytes(str);

            return (_socketClient?.Send(data) == data.Length);
        }
        catch (SocketException ex)
        {
            if (NetworkErrorEvent is not null && !IsConnected())
            {
                NetworkErrorEvent(this, ex.SocketErrorCode);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("send error:" + ex.ToString());
        }
        return false;
    }

    protected string WaitReply(int timeoutMillisecond)
    {
        try
        {
            if (_socketClient is null) return string.Empty;

            if (timeoutMillisecond != _socketClient.ReceiveTimeout)
            {
                _socketClient.ReceiveTimeout = timeoutMillisecond;
            }
            //byte[] buffer = new byte[1024];
            byte[] buffer = new byte[64];
            int length = _socketClient.Receive(buffer);
            string str = Encoding.UTF8.GetString(buffer, 0, length);

            //////////////////////
            System.Diagnostics.Debug.WriteLine("DOBOT_in: " + str);

            return str;
        }
        catch (SocketException ex)
        {
            if (NetworkErrorEvent is not null && !IsConnected())
            {
                NetworkErrorEvent(this, ex.SocketErrorCode);
            }

            //////////////////////////////////////////////////////////if(!_socketClient.Connected)
            //////////////////////////////////////////////////////////{
            //////////////////////////////////////////////////////////    System.Diagnostics.Debug.WriteLine($"_socketClient not connected, try to reconnect: IP {0}, Port {1})", _ipAddress, _port);
            //////////////////////////////////////////////////////////    this.ConnectDobotServer(_ipAddress, _port);
            //////////////////////////////////////////////////////////}
            return "send error:" + ex.Message;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Send error: {ex.Message}");
            return $"Send error: {ex.Message}";
        }
    }

    protected int Receive(byte[] buffer, int offset, int size, SocketFlags flag)
    {
        if (_socketClient is null) return 0;

        try
        {
            int numberOfBytes = _socketClient.Receive(buffer, offset, size, flag);

            if (numberOfBytes == 0)
            {
                if (NetworkErrorEvent is not null && !IsConnected())
                {
                    NetworkErrorEvent(this, SocketError.ConnectionAborted);
                }
            }

            return numberOfBytes;
        }
        catch (SocketException ex)
        {
            if (NetworkErrorEvent is not null&& !IsConnected())
            {
                NetworkErrorEvent(this, ex.SocketErrorCode);
            }
            return -1;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Receive error: {ex.Message}");
        }
        return 0;
    }
}
