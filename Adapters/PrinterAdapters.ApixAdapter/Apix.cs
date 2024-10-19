using Entities;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using UseCases.core;

namespace PrinterAdapters.ApixAdapter;

public class Apix : IPrinterService
{
    private string _ipAddress;
    private int _port;
    private bool _debugMode;
    private bool _isConnected = false;
    private string _currentLabel = string.Empty;
    private const char ESC = (char)27;
    private const char STX = (char)2;
    private const char ETX = (char)3;
    private const char CR = (char)13;
    private const char LF = (char)10;
    private const int StatusLength = 4;
    private TcpClient client;
    private NetworkStream stream;

    private readonly Dictionary<char, string> Messages = new()
    {
        {'@', "Normal"},
        {'`', "Pause"},
        {'B', "Backing label"},
        {'C', "Cutting"},
        {'E', "Printer Error" },
        {'F', "Form Feed" },
        {'K', "Waiting to press print key" },
        {'L', "Waiting to take label" },
        {'P', "Printing batch" },
        {'W', "Imaging" }
    };

    private readonly Dictionary<char, string> Warnings = new()
    {
        {'@', "Normal"},
        {'A', "Paper low"},
        {'B', "Ribbon low"}
    };

    private readonly Dictionary<char, string> Error1 = new()
    {
        {'@', "Normal"},
        {'A', "Print head overheat"},
        {'B', "Stepping motor overheat"},
        {'D', "Print head error"},
        {'H', "Cutter jam" },
        {'P', "Insufficient memory" }
    };

    private readonly Dictionary<char, string> Error2 = new()
    {
        {'@', "Normal"},
        {'A', "Paper empty"},
        {'B', "Paper jam"},
        {'D', "Ribbon empty"},
        {'H', "Ribbon jam" },
        {'`', "Print head open" },
    };

    public bool IsConnected {
        get {
            return _isConnected;
        }
    }

    public void Connect(string ipAddress, int port, bool debugMode)
    {
        _ipAddress = ipAddress;
        _port = port;
        _debugMode = debugMode;
        if (_debugMode) return;

        try
        {
            client = new TcpClient(_ipAddress, _port);
            stream = client.GetStream(); 
            _isConnected = true;

        }
        catch (Exception ex)
        {
            _isConnected = false;
        }
    }

    public void Disconnect()
    {
        if (_debugMode) return;
        _isConnected = false;
    }

    public OperationStatus FormFeed()
    {
        if (_debugMode) return OperationStatus.OK;
        Send("FORMFEED");
        return OperationStatus.OK;
    }

    public OperationStatus Print()
    {
        if (_debugMode) return OperationStatus.OK;

        if (string.IsNullOrEmpty(_currentLabel))
            return OperationStatus.LabelNotLoaded;

        SendData(_currentLabel);
        var stopWatch = Stopwatch.StartNew();
        bool isLabelPrinted = false;

        while (!isLabelPrinted && stopWatch.ElapsedMilliseconds < 10 * 1000)
        {
            isLabelPrinted = GetStatus() == PrinterStatus.LabelPendingOnSensor;
            Thread.Sleep(100);
        }
        return isLabelPrinted ? OperationStatus.OK : OperationStatus.LabelNotPrinted;
    }

    public OperationStatus Load(Dictionary<string, string> parameters)
    {
        if (_debugMode) return OperationStatus.OK;
        
        if (parameters.ContainsKey("LABEL"))
        {
            _currentLabel = parameters["LABEL"];
        }

        return OperationStatus.OK;
    }

    public PrinterStatus GetStatus()
    {
        string apixStatus = SendDataAndReceive($"{ESC}!S");
        string parsedStatus = apixStatus
            .Replace(STX.ToString(), string.Empty)
            .Replace(ETX.ToString(), string.Empty)
            .Replace(CR.ToString(), string.Empty)
            .Replace(LF.ToString(), string.Empty);

        if (parsedStatus.Length != StatusLength) return PrinterStatus.Error;

        if (parsedStatus.First() == Messages.First(x => x.Value == "Waiting to take label").Key)
        {
            return PrinterStatus.LabelPendingOnSensor;
        }
        else if (parsedStatus[1] == Warnings.First(x => x.Value == "Paper low").Key)
        {
            return PrinterStatus.PaperLow;
        }
        else if (parsedStatus[1] == Warnings.First(x => x.Value == "Ribbon low").Key)
        {
            return PrinterStatus.RibbonLow;
        }
        else
        {
            return PrinterStatus.OK;
        }
    }

    private void SendData(string message)
    {
        if (client == null || !client.Connected)
        {
            Connect(_ipAddress, _port, _debugMode);
        }

        try
        {
            byte[] dataToSend = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            stream.Write(dataToSend, 0, dataToSend.Length);
        }
        catch (Exception ex)
        {
        }
    }

    private string SendDataAndReceive(string message)
    {
        if (client == null || !client.Connected)
        {
            Connect(_ipAddress, _port, _debugMode);
        }

        try
        {
            byte[] dataToSend = Encoding.ASCII.GetBytes(message + Environment.NewLine);
            stream.Write(dataToSend, 0, dataToSend.Length);
            byte[] receivedBytes = new byte[1024];

            int bytesRead = stream.Read(receivedBytes, 0, receivedBytes.Length);
            string response = Encoding.ASCII.GetString(receivedBytes, 0, bytesRead);
            return response;
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    public OperationStatus Send(string buffer)
    {
        if (_debugMode) return OperationStatus.OK;

        SendData(buffer);
        var stopWatch = Stopwatch.StartNew();
        bool isLabelPrinted = false;

        while (!isLabelPrinted && stopWatch.ElapsedMilliseconds < 10 * 1000)
        {
            isLabelPrinted = GetStatus() == PrinterStatus.LabelPendingOnSensor;
            Thread.Sleep(1000);
        }

        return isLabelPrinted ? OperationStatus.OK : OperationStatus.LabelNotPrinted;
    }

    public OperationStatus Reset()
    {
        if (_debugMode) return OperationStatus.OK;

        return GetStatus() == PrinterStatus.OK ? OperationStatus.OK : OperationStatus.Error;
    }
}
