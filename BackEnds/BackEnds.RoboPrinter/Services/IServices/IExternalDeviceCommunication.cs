using Entities;

namespace BackEnds.RoboPrinter.Services.IServices;

public interface IExternalDeviceCommunication
{
    event EventHandler<ExternalDeviceEventArgs> UpdateSerialNumberRequested;
    event EventHandler<ExternalDeviceEventArgs> LoadProductRequested;
    event EventHandler PrintRequested;
    event EventHandler<ExternalDeviceEventArgs> PickLabelRequested;
    event EventHandler<ExternalDeviceEventArgs> ApplyLabelRequested;
    event EventHandler<ExternalDeviceEventArgs> DirectMessageToPrinterRequested;
    event EventHandler StatusRequested;
    event EventHandler ResetRequested;
    void Connect();
    void Disconnect();
    void Load(int activeOperativeMode);
    void Reset();
    void SendStatus(SystemStatus status);
}
