#region Imports

using BackEnds.RoboPrinter.RobotModels;
using BackEnds.RoboPrinter.Services;
using BackEnds.RoboPrinter.Services.IServices;
using Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;
using UseCases.core;

#endregion

namespace BackEnds.RoboPrinter;

public class Supervisor : IHostedService
{
    private readonly ILogger<Supervisor> _logger;
    private readonly IRobotService _robotService;
    private readonly IPrinterService _printerService;
    private readonly IExternalDeviceCommunication _externalDevice;
    private readonly IConfiguration _configuration;
    private readonly ViewModel _viewModel;
    private readonly IOExternalCommunication _ioExternalCommunication;
    private readonly GPIOManager _gpioManager;
    private readonly ICycleService _cycleService;
    private int _activeOperativeModeId;
    private string _currentSerialNumber = string.Empty;
    private bool _areDigitalIOSignalsEnabled = false;
    private bool _executeEntireCycleEnabled = false;
    private string _pendingLabelToPrint = string.Empty;

    public Supervisor(ILogger<Supervisor> logger, IRobotService robotService, IPrinterService printerService, IExternalDeviceCommunication externalDevice,
        IConfiguration configuration, ViewModel viewModel, IOExternalCommunication ioExternalCommunication, GPIOManager gpioManager, ICycleService cycleService)
    {
        _logger = logger;
        _robotService = robotService;
        _printerService = printerService;
        _externalDevice = externalDevice;
        _configuration = configuration;
        _viewModel = viewModel;
        _ioExternalCommunication = ioExternalCommunication;
        _gpioManager = gpioManager;
        _cycleService = cycleService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Supervisor starting...");
        _activeOperativeModeId = await _viewModel.GetActiveOperativeMode();
        _areDigitalIOSignalsEnabled = await _viewModel.GetDigitalIOSignalsConfiguration();
        _executeEntireCycleEnabled = await _viewModel.GetCycleConfiguration();
        await _viewModel.GetActiveProduct();
        await _viewModel.GetRobotOverride();
        await InitializeRobot();
        _printerService.Connect(_configuration["InternalDevices:Printer:IpAddress"], Convert.ToInt16(_configuration["InternalDevices:Printer:Port"]), Convert.ToBoolean(_configuration["InternalDevices:Robot:DebugMode"]));
        InitializeExternalDevice();
        InitializeIOCommunication();
    }

    private async Task InitializeRobot()
    {
        bool isRobotInDebugMode = Convert.ToBoolean(_configuration["InternalDevices:Robot:DebugMode"]);
        if (isRobotInDebugMode) return;
        _logger.LogInformation("Supervisor - Initializing Robot...");

        string robotIpAddress = _configuration["InternalDevices:Robot:IpAddress"] ?? string.Empty;
        var ping = new Ping();

        while (ping.Send(robotIpAddress).Status != IPStatus.Success)
        {
            _logger.LogInformation("Robot ping failed - Trying to turn it on with GPIO output...");
            await Task.Delay(500);
            _logger.LogInformation("Robot ping failed - Set Output to FALSE...");
            _gpioManager.SetDigitalOutput(1, false);
            await Task.Delay(500);
            _logger.LogInformation("Robot ping failed - Set Output to TRUE...");
            _gpioManager.SetDigitalOutput(1, true);
            _logger.LogInformation("Robot ping failed - Waiting 10 sec...");
            await Task.Delay(10000);
        }

        _logger.LogInformation("Connecting to Robot...");

        _robotService.Connect(robotIpAddress, Convert.ToInt16(_configuration["InternalDevices:Robot:Port"]), isRobotInDebugMode);

        var parameters = new Dictionary<string, string>
        {
            { "MOVEMENTTOLERANCE", _configuration["InternalDevices:Robot:MovementTolerance"] ?? "0" }
        };

        _robotService.Load(parameters);
        _robotService.SetSpeedRatio(_viewModel.RobotOverride);
        /////////////_robotService.SetUserFrame(0, 0, 0, 0, 0);
        ////////////_robotService.SetToolFrame(0, 0, 0, 0, 0);
    }

    private void InitializeExternalDevice()
    {
        if (_externalDevice is null) return;

        _externalDevice.PrintRequested += OnCSEPrintRequested;
        _externalDevice.ResetRequested += OnResetRequested;
        _externalDevice.StatusRequested += OnStatusRequested;
        _externalDevice.DirectMessageToPrinterRequested += OnDirectMessageToPrinterRequested;
        _externalDevice.UpdateSerialNumberRequested += OnUpdateSerialNumberRequested;
        _externalDevice.LoadProductRequested += OnLoadProductRequested;
        _externalDevice.Load(_activeOperativeModeId);
        _externalDevice.Connect();
        _logger.LogInformation($"InitializeExternalDevice - connection completed");
    }

    private void InitializeIOCommunication()
    {
        _ioExternalCommunication.PrintRequested += OnPrintRequested;
        _ioExternalCommunication.ApplyRequested += OnApplyRequested;
        _ioExternalCommunication.HomePositionRequested += OnHomePositionRequested;
        _ioExternalCommunication.ResetRequested += OnResetRequested;
        _ioExternalCommunication.SetProgram += OnSetProgram;
        _ioExternalCommunication.Connect();
        _logger.LogInformation($"InitializeIOCommunication - connection completed");
        
        if (Convert.ToBoolean(_configuration["DigitalOutputsEnabled"]))
        {
			// Gestione segnale di ready
			_robotService.SetDigitalOutput(DigitalOutputs.Ready, true, true);
		}
    }

    #region EventHandlers

    private async void OnPrintRequested(object? sender, EventArgs e)
    {
        ///Funzione che si attiva alla ricezione del comando da plc
        _areDigitalIOSignalsEnabled = await _viewModel.GetDigitalIOSignalsConfiguration();
        if (_areDigitalIOSignalsEnabled != true)
        {
            _logger.LogInformation("OnPrintRequested - GetDigitalIOSignalsConfiguration is off!");
            return;
        }

        /////////////////////////////////////////////////////////////////////
        _ioExternalCommunication.ResetLabelSignals();



        _executeEntireCycleEnabled = await _viewModel.GetCycleConfiguration();

        _logger.LogInformation("OnPrintRequested - Active OperativeMode: {OperativeMode}", (OperativeModes)_activeOperativeModeId);

        _activeOperativeModeId = await _viewModel.GetActiveOperativeMode();

        if (_activeOperativeModeId == (int)OperativeModes.Manual)
        {
            _logger.LogInformation("OnPrintRequested - ActiveOperativeMode is manual!");

            await _viewModel.GetActiveProduct();

            var status = await _cycleService.ExecutePrintCycle(_viewModel.ActiveProductId, "99999999", _activeOperativeModeId, string.Empty);

            if (status == OperationStatus.OK)
            {
                _ioExternalCommunication.LabelPrinted(status);

                if (_executeEntireCycleEnabled)
                {
                    var statusApply = await _cycleService.ExecuteApplyCycle(_viewModel.ActiveProductId, "99999999", _activeOperativeModeId);

                    if (statusApply == OperationStatus.OK)
                    {
                        _ioExternalCommunication.LabelApplied(statusApply);
                    }
                }
            }
            return;
        }
        else
        {
            bool successful = await ExecutePrintCycle();

            if (successful && _executeEntireCycleEnabled)
            {
                await ExecuteApplyCycle();
            }
        }

    }

    private async void OnApplyRequested(object? sender, EventArgs e)
    {
        _areDigitalIOSignalsEnabled = await _viewModel.GetDigitalIOSignalsConfiguration();
        if (_areDigitalIOSignalsEnabled != true)
        {
            _logger.LogInformation("OnPrintRequested - GetDigitalIOSignalsConfiguration is off!");
            return;
        }

        _executeEntireCycleEnabled = await _viewModel.GetCycleConfiguration();

        if (_executeEntireCycleEnabled)
        {
            _logger.LogInformation("OnApplyRequested - EntireCycle logic is enabled!");
            return;
        }

        _activeOperativeModeId = await _viewModel.GetActiveOperativeMode();

        if (_activeOperativeModeId == (int)OperativeModes.Manual)
        {
            _logger.LogInformation("OnApplyRequested - ActiveOperativeMode is manual!");

            var statusApply = await _cycleService.ExecuteApplyCycle(_viewModel.ActiveProductId, "99999999", _activeOperativeModeId);

            if (statusApply == OperationStatus.OK)
            {
                _ioExternalCommunication.LabelApplied(statusApply);
            }
            return;
        }

        _logger.LogInformation("OnApplyRequested - Active OperativeMode: {OperativeMode}", (OperativeModes)_activeOperativeModeId);
        await ExecuteApplyCycle();
    }

    private async void OnSetProgram(object? sender, EventArgs e)
    {
        _logger.LogInformation("OnSetProgram");
        int i = (_robotService.ReadDigitalInput(DigitalInputs.Typology2) ? 1 : 0) << 2 | (_robotService.ReadDigitalInput(DigitalInputs.Typology1) ? 1 : 0) << 1 | (_robotService.ReadDigitalInput(DigitalInputs.Typology0) ? 1 : 0);

        var product = await _viewModel.GetProductByDescription("Template0" + i);

        if (product is not null)
        {
            _logger.LogError("Found a Product with Description: {Description} - Updating ActiveProduct...", product);
            await _viewModel.SaveActiveProduct(product.Id);
        }
        else
        {
            _logger.LogError("Failed to find a Product with Id or Description equal to: {Product}", product);
        }
    }


    private async void OnCSEPrintRequested(object? sender, EventArgs e)
    {
        _activeOperativeModeId = await _viewModel.GetActiveOperativeMode();

        if (_activeOperativeModeId == (int)OperativeModes.Manual)
        {
            _logger.LogInformation("OnPrintRequested - ActiveOperativeMode is manual!");
            return;
        }

        bool successful = await ExecutePrintCycle();

        if (successful && _executeEntireCycleEnabled)
        {
            await ExecuteApplyCycle();
        }
    }

    private async void OnDirectMessageToPrinterRequested(object? sender, ExternalDeviceEventArgs e)
    {
        _activeOperativeModeId = await _viewModel.GetActiveOperativeMode();

        if (_activeOperativeModeId == (int)OperativeModes.Manual)
        {
            _logger.LogInformation("OnDirectMessageToPrinterRequested - ActiveOperativeMode is manual!");
            return;
        }

        _logger.LogInformation("OnDirectMessageToPrinterRequested - Received Message {Message}..." , e.Content);
        _pendingLabelToPrint = e.Content;
        _currentSerialNumber = "99999999";

        _areDigitalIOSignalsEnabled = await _viewModel.GetDigitalIOSignalsConfiguration();

        _ioExternalCommunication.ResetLabelSignals();

        if (!_areDigitalIOSignalsEnabled)
        {
            bool successful = await ExecutePrintCycle();

            if (successful)
            {
                await ExecuteApplyCycle();
            }
        }
        else
        {
            /////////////////////////////////////////_ioExternalCommunication.ResetLabelSignals();    
        }
    }

    private void OnUpdateSerialNumberRequested(object? sender, ExternalDeviceEventArgs e)
    {
        _logger.LogInformation("OnUpdateSerialNumberRequested - Received SerialNumber {SerialNumber}...", e.Content);
        _currentSerialNumber = e.Content;
    }

    private async void OnLoadProductRequested(object? sender, ExternalDeviceEventArgs e)
    {
        _logger.LogInformation("OnLoadProductRequested - Received ProductId {ProductId}...", e.Content);
        
        if (int.TryParse(e.Content, out int newProductId))
        {
            var product = await _viewModel.GetProductById(newProductId);

            if (product is not null)
            {
                _logger.LogError("Found a Product with Id: {Id} - Updating ActiveProduct...", newProductId);
                await _viewModel.SaveActiveProduct(newProductId);
            }
        }
        else
        {
            var product = await _viewModel.GetProductByDescription(e.Content);

            if (product is not null)
            {
                _logger.LogError("Found a Product with Description: {Description} - Updating ActiveProduct...", e.Content);
                await _viewModel.SaveActiveProduct(product.Id);
            }
            else
            {
                _logger.LogError("Failed to find a Product with Id or Description equal to: {Product}", e.Content);
            }
        }
    }

    private void OnHomePositionRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("OnHomePositionRequested");
        //_externalDevice?.Reset();
        //_ioExternalCommunication.Reset();
        _cycleService.ReturnToHome();
    }

    private async void OnResetRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("OnResetRequested");
        //_externalDevice?.Reset();
        //_ioExternalCommunication.Reset();
        _robotService.FullReset();
        _printerService.Reset();
        var printerStatus = _printerService.GetStatus();
        _logger.LogInformation("Printer Reset returned status: {PrinterStatus}", printerStatus);

        if (printerStatus == PrinterStatus.PaperLow || printerStatus == PrinterStatus.RibbonLow)
        {
            _robotService.SetDigitalOutput((int)DigitalOutputs.PrinterLowMaterial, true);
        }
        else if (printerStatus == PrinterStatus.OK)
        {
            _robotService.SetDigitalOutput((int)DigitalOutputs.PrinterLowMaterial, false);
        }

        _robotService.SetSpeedRatio(_viewModel.RobotOverride);
    }

    private async void OnStatusRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("OnStatusRequested");
        var printerStatus = _printerService.GetStatus();
        var robotStatus = _robotService.Status;
        var systemStatus = printerStatus == PrinterStatus.OK && (robotStatus == "Enabled" ||  robotStatus == "Running") ? SystemStatus.OK : SystemStatus.Error;
        _logger.LogInformation("Printer Reset returned status: {PrinterStatus}", printerStatus);
        _externalDevice.SendStatus(systemStatus);
    }

    #endregion

    private async Task<bool> ExecutePrintCycle()
    {
        if (_viewModel.ActiveProductId == 0)
        {
            _logger.LogInformation("ExecutePrintCycle - ActiveProduct is zero!");
            return false;
        }

        if (_activeOperativeModeId == 0)
        {
            _activeOperativeModeId = await _viewModel.GetActiveOperativeMode();

            if (_activeOperativeModeId == 0)
            {
                _logger.LogInformation("ExecutePrintCycle - ActiveOperativeMode is zero!");
                return false;
            }
            else if (_activeOperativeModeId == (int)OperativeModes.Manual)
            {
                _logger.LogInformation("ExecutePrintCycle - ActiveOperativeMode is manual!");
                return false;
            }
        }

        var status = await _cycleService.ExecutePrintCycle(_viewModel.ActiveProductId, _currentSerialNumber, _activeOperativeModeId, _pendingLabelToPrint);
        _pendingLabelToPrint = string.Empty;
        _ioExternalCommunication.LabelPrinted(status);
        return status == OperationStatus.OK;
    }

    private async Task ExecuteApplyCycle()
    {
        if (_viewModel.ActiveProductId == 0)
        {
            _logger.LogInformation("ExecuteApplyCycle - ActiveProduct is zero!");
            return;
        }

        if (_activeOperativeModeId == 0)
        {
            _activeOperativeModeId = await _viewModel.GetActiveOperativeMode();

            if (_activeOperativeModeId == 0)
            {
                _logger.LogInformation("ExecuteApplyCycle - ActiveOperativeMode is zero!");
                return;
            }
            else if (_activeOperativeModeId == (int)OperativeModes.Manual)
            {
                _logger.LogInformation("ExecuteApplyCycle - ActiveOperativeMode is manual!");
                return;
            }
        }

        var status = await _cycleService.ExecuteApplyCycle(_viewModel.ActiveProductId, _currentSerialNumber, _activeOperativeModeId);
        _pendingLabelToPrint = string.Empty;
        _ioExternalCommunication.LabelApplied(status);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _robotService.Disconnect();
        _printerService.Disconnect();
        return Task.CompletedTask;
    }
}
