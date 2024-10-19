using Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO.Ports;
using System.Text;

namespace BackEnds.RoboPrinter.Services;

public class SerialExternalCommunication : ExternalCommunicationBase
{
    public event EventHandler<ExternalDeviceEventArgs> PickLabelRequested;
    public event EventHandler<ExternalDeviceEventArgs> ApplyLabelRequested;
    public event EventHandler<ExternalDeviceEventArgs> PrintLabelRequested;
    public event EventHandler ResetRequested;

    private readonly SerialPort _serialPort;
    private readonly ILogger<SerialExternalCommunication> _logger;
    private const int BaudRate = 9600;
    private const int DataBits = 8;
    private readonly Parity Parity = Parity.None;
    private readonly StopBits StopBits = StopBits.One;
    private StringBuilder _buffer = new();

    public SerialExternalCommunication(IConfiguration configuration, ILogger<SerialExternalCommunication> logger)
    {
        _logger = logger;
        _serialPort = new SerialPort()
        {
            PortName = $"COM{configuration["ExternalDevices:SerialCOM:Port"]}",
            BaudRate = BaudRate,
            Parity = Parity,
            DataBits = DataBits,
            StopBits = StopBits
        };
        _serialPort.DataReceived += OnDataReceived;
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        string inputData = _serialPort.ReadExisting();
        _buffer.Append(inputData);

        if (_buffer.ToString().IndexOf(EndBuffer) >= 0)
        {
            ProcessData(_buffer.ToString());
            _buffer.Clear();
        }
    }

    public override void Connect()
    {
        _logger.LogInformation("SerialExternalCommunication - Opening Port...");
        _serialPort.Open();
    }

    public override void Disconnect()
    {
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
        _serialPort.Close();
        _serialPort.Dispose();
    }

    public override void Reset()
    {
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
        _buffer.Clear();
    }

    public override void SendStatus(OperationStatus status)
    {
        SendMessage(status.ToString());
    }

    private void SendMessage(string message)
    {
        if (_serialPort.IsOpen)
        {
            _logger.LogInformation("SerialExternalCommunication - Sending Message: {Message}", message);
            _serialPort.Write(message);
        }
    }
}
