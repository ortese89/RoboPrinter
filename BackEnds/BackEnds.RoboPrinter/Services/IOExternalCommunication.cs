using Entities;
using UseCases.core;
using BackEnds.RoboPrinter.RobotModels;

namespace BackEnds.RoboPrinter.Services;

public class IOExternalCommunication
{
    public event EventHandler PrintRequested;
    public event EventHandler ApplyRequested;
    public event EventHandler HomePositionRequested;
    public event EventHandler ResetRequested;
    private CancellationTokenSource _cts;
    private readonly Dictionary<int, bool> _previousStates = [];
    private readonly IRobotService _robotService;

    public IOExternalCommunication(IRobotService robotService)
    {
        _robotService = robotService;
        InitializeInputDigitalsStates();
    }

    public void Connect()
    {
        _cts = new();
        Task.Run(CheckRisingEdges);
    }

    public void Disconnect()
    {
        _cts.Cancel();
    }

    private void InitializeInputDigitalsStates()
    {
        var properties = typeof(DigitalInputs).GetProperties()
            .Where(x => x.PropertyType == typeof(int));

        foreach (var property in properties)
        {
            int signalIndex = (int)property.GetValue(null);
            _previousStates[signalIndex] = false;
        }
    }


    public void ResetLabelSignals()
    {
        _robotService.SetDigitalOutput(DigitalOutputs.LabelPrinted, false);
        _robotService.SetDigitalOutput(DigitalOutputs.LabelApplied, false);
    }

    public void LabelPrinted(OperationStatus status)
    {
        if (status == OperationStatus.OK)
        {
            _robotService.SetDigitalOutput(DigitalOutputs.LabelPrinted, true);
        }
        //_robotService.SetDigitalOutput(DigitalOutputs.Ready, status == OperationStatus.OK, true);
    }

    public void LabelApplied(OperationStatus status)
    {
        if (status == OperationStatus.OK)
        {
            _robotService.SetDigitalOutput(DigitalOutputs.LabelApplied, true);
        }
        //_robotService.SetDigitalOutput(DigitalOutputs.Ready, status == OperationStatus.OK, true);
    }

    public void Reset()
    {
        InitializeInputDigitalsStates();
    }

    private async Task CheckRisingEdges() 
    {
        var properties = typeof(DigitalInputs).GetProperties()
            .Where(x => x.PropertyType == typeof(int));

        while (!_cts.IsCancellationRequested)
        {
            foreach (var property in properties)
            {
                int signalIndex = (int)property.GetValue(null);
                CheckSignal(signalIndex);
            }

            await Task.Delay(15);
        }
    }

    private void CheckSignal(int signalIndex)
    {
        bool currentState = _robotService.ReadDigitalInput(signalIndex);

        if (_previousStates.TryGetValue(signalIndex, out bool previousState))
        {
            if (currentState && !previousState)
            {
                // Rising edge
                OnRisingEdge(signalIndex);
            }
            else if (!currentState && previousState)
            {
                // Falling edge
                OnFallingEdge(signalIndex);
            }
        }

        _previousStates[signalIndex] = currentState;
    }

    private void OnRisingEdge(int signalIndex)
    {
        switch (signalIndex)
        {
            case int index when index == DigitalInputs.PrintCommand:
                PrintRequested?.Invoke(this, new ExternalDeviceEventArgs());
                break;
            case int index when index == DigitalInputs.ApplyCommand:
                ApplyRequested?.Invoke(this, new ExternalDeviceEventArgs());
                break;
            case int index when index == DigitalInputs.HomeCommand:
                HomePositionRequested?.Invoke(this, new ExternalDeviceEventArgs());
                break;
            case int index when index == DigitalInputs.ResetCommand:
                ResetRequested?.Invoke(this, EventArgs.Empty);
                break;
            case int index when index == DigitalInputs.Typology0:
                break;
            case int index when index == DigitalInputs.Typology1:
                break;
            case int index when index == DigitalInputs.Typology2:
                break;
            default:
                break;
        }
    }
    private void OnFallingEdge(int signalIndex)
    {
        switch (signalIndex)
        {
            case int index when index == DigitalInputs.PrintCommand:
                break;
            case int index when index == DigitalInputs.ApplyCommand:
                break;
            case int index when index == DigitalInputs.HomeCommand:
                break;
            case int index when index == DigitalInputs.ResetCommand:
                break;
            case int index when index == DigitalInputs.Typology0:
                break;
            case int index when index == DigitalInputs.Typology1:
                break;
            case int index when index == DigitalInputs.Typology2:
                break;
            default:
                break;
        }
    }

    private int GetTypology()
    {
        return (_robotService.ReadDigitalInput(DigitalInputs.Typology2) ? 1 : 0) << 2 | (_robotService.ReadDigitalInput(DigitalInputs.Typology1) ? 1 : 0) << 1 | (_robotService.ReadDigitalInput(DigitalInputs.Typology0) ? 1 : 0);
    }
}
