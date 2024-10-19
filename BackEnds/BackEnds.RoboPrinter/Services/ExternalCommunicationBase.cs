﻿using Entities;
using BackEnds.RoboPrinter.Services.IServices;

namespace BackEnds.RoboPrinter.Services;

public abstract class ExternalCommunicationBase : IExternalDeviceCommunication
{
    public event EventHandler PrintRequested;
    public event EventHandler<ExternalDeviceEventArgs> PickLabelRequested;
    public event EventHandler<ExternalDeviceEventArgs> ApplyLabelRequested;
    public event EventHandler ResetRequested;
    public event EventHandler<ExternalDeviceEventArgs> UpdateSerialNumberRequested;
    public event EventHandler<ExternalDeviceEventArgs> DirectMessageToPrinterRequested;
    public event EventHandler StatusRequested;

    private const char CR = (char)13;
    private const char LF = (char)10;
    protected readonly char Separator = '|';
    protected readonly string StartBuffer = "^";
    protected readonly string EndBuffer = $"{CR}{LF}";

    protected readonly Dictionary<string, string> CSECommands = new()
    {
        {"^!", "Conditional print command or reprocessing of printing memory."},
        {"^#00", "Set Progressive Number"},
        {"^#01", "Set Decrementing Number"},
        {"^#02", "Set the Date"},
        {"^#03", "Set the Time" },
        {"^#05", "Increment Frequency" },
        {"^#07", "Initialize Progressive Number" },
        {"^&", "Switch between READY and Manual" },
        {"^*", "Unconditional execution command of the application procedure" },
        {"^?", "Status Request" },
        {"^@", "Reset and page initialization" },
        {"^A", "Recall a page from drive" },
        {"^A*", "Recall a page from drive (UTF8)" },
        {"^|i ", "Unput of Variable Data from Serial in a previously loaded Format Page" },
        {"^V", "Processing and generation of a page ready for printing" },
        {"^X", "Reset command. Exit from fault error situation" },
        {"^XX", "Reset command. Exit from fault error situation with Zebra emulation" },
        {"^z", "Reprocessing, printing and application for 3 sides applicators" },
        {"^Z", "Application only command for 3 sides applicators" },
    };

    protected void ProcessData(string inputData)
    {
        if (IsCSEProtocolMessage(inputData))
        {
            ProcessCSEMessage(inputData);
        }
        else
        {
            DirectMessageToPrinterRequested?.Invoke(this, new ExternalDeviceEventArgs(inputData));
        }
    }

    private bool IsCSEProtocolMessage(string message)
    {
        return message.StartsWith(StartBuffer) && message.EndsWith(EndBuffer);
    }

    private void ProcessCSEMessage(string message)
    {
        var matchedCommand = CSECommands.Keys.FirstOrDefault(k => message.StartsWith(k));

        if (matchedCommand is null) return;

        switch (matchedCommand)
        {
            case "^!":
                PrintRequested?.Invoke(this, new ExternalDeviceEventArgs());
                break;
            case "^#00":
                string serialNumber = message.Replace(matchedCommand, string.Empty).Replace(EndBuffer, string.Empty);
                UpdateSerialNumberRequested?.Invoke(this, new ExternalDeviceEventArgs(serialNumber));
                break;

            case "^@":
            case "^X":
                ResetRequested?.Invoke(this, new ExternalDeviceEventArgs());
                break;

            case "^?":
                StatusRequested?.Invoke(this, new ExternalDeviceEventArgs());
                break;

            default: break;
        }
    }

    public abstract void Connect();

    public abstract void Disconnect();

    public abstract void SendStatus(OperationStatus status);

    public abstract void Reset();
}
