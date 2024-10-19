using Entities;

namespace UseCases.core;

public interface IPrinterService
{
    bool IsConnected { get; }
    void Connect(string ipAddress, int port, bool debugMode);
    void Disconnect();
    PrinterStatus GetStatus();
    OperationStatus Reset();
    OperationStatus Load(Dictionary<string, string> parameters);
    OperationStatus Send(string message);
    OperationStatus Print();
    OperationStatus FormFeed();
}
