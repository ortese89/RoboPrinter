#region Imports

using Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BackEnds.RoboPrinter.Models;
using BackEnds.RoboPrinter.Models.Dto;
using BackEnds.RoboPrinter.RobotModels;
using BackEnds.RoboPrinter.Services.IServices;
using UseCases.core;
using static UseCases.core.IRobotService;
using System.Net.NetworkInformation;
using BackEnds.RoboPrinter.Services;
using Microsoft.AspNetCore.Identity;

#endregion

namespace BackEnds.RoboPrinter;

public class Controller
{
    private readonly ILogger<Controller> _logger;
    private readonly IRobotService _robotService;
    private readonly IPrinterService _printerService;
    private readonly IConfiguration _configuration;
    private readonly ViewModel _viewModel;
    private readonly ICycleService _cycleService;
    private readonly IOExternalCommunication _ioExternalCommunication;
    private readonly IExternalDeviceCommunication _externalDeviceCommunication;
    private readonly GPIOManager _gpioManager;
    private bool _useDigitalIO;
    private int _activeOperativeModeId = 0;
    private bool _areDigitalIOSignalsEnabled = false;
    private bool _isExecuteEntireCycleEnabled = false;
    private string _manualSerialNumber = string.Empty;
    private RobotPosition? _applicationRouteHomePosition;
    public bool IsCycling { get; private set; } = false;

    public Controller(ILogger<Controller> logger, IRobotService robotService, IPrinterService printerService,
        IConfiguration configuration, ViewModel viewModel, ICycleService cycleService, 
        GPIOManager gpioManager, IOExternalCommunication ioExternalCommunication, IExternalDeviceCommunication externalDeviceCommunication)
    {
        _logger = logger;
        _robotService = robotService;
        _printerService = printerService;
        _configuration = configuration;
        _viewModel = viewModel;
        _cycleService = cycleService;
        _gpioManager = gpioManager;
        _ioExternalCommunication = ioExternalCommunication;
        Task.Run(CheckHomePosition);
        _externalDeviceCommunication = externalDeviceCommunication;
        MoveJog(JogMovement.X, true);
        StopJog();
    }

    public async Task AddNewRouteStep(RouteStepDto routeStepDto)
    {
        var pointTypes = await _viewModel.GetPointTypes();
        int? clearancePointTypeId = pointTypes.FirstOrDefault(x => x.Description.Contains("Clearance"))?.Id;
        var clearancePoint = new RobotPoint()
        {
            X = routeStepDto.RobotPoint.X,
            Y = routeStepDto.RobotPoint.Y,
            Z = routeStepDto.RobotPoint.Z,
            Yaw = routeStepDto.RobotPoint.Yaw,
            Pitch = routeStepDto.RobotPoint.Pitch,
            Roll = routeStepDto.RobotPoint.Roll,
            PointTypeId = clearancePointTypeId ?? 3
        };

        var routeStep = new RouteStep()
        {
            RouteId = routeStepDto.RouteId,
            RobotPoint = clearancePoint,
            StepOrder = routeStepDto.StepOrder + 1,
            Speed = routeStepDto.Speed,
            ClearZone = routeStepDto.ClearZone
        };
        await _viewModel.AppendNewRouteStep(routeStep);
    }

    public async Task<(IdentityUser User, IList<string> Roles)> Login(string username, string password)
    {
        var (User, Roles) = await _viewModel.Login(username, password);

        //if (User is not null && Roles is not null)
        //{

        //}
        return (User, Roles);
            //return _jwtTokenGenerator.GenerateToken(User, Roles);

        //return <(null, null>);
    }

    public void MoveJog(JogMovement jogMovement, bool forward)
    {
        _robotService.MoveJog(jogMovement, forward);
    }

    public void StopJog()
    {
        _robotService.StopJog();
    }

    public async Task ReturnToHomePosition()
    {
        await _cycleService.ReturnToHome();
    }

    public async Task CloneProduct(int productId, string productName)
    {
        await _viewModel.CloneProduct(productId, productName);
    }

    public async Task<OperativeModeDto[]> GetOperativeModes()
    {
        var operativeModes = await _viewModel.GetOperativeModes();
        return operativeModes.Select(x => new OperativeModeDto()
        {
            Id = x.Id,
            Description = x.Description
        })
        .ToArray();
    }

    public async Task<int> GetActiveOperativeMode()
    {
        _activeOperativeModeId = await _viewModel.GetActiveOperativeMode();
        return _activeOperativeModeId;
    }

    public async Task<int> GetActiveProduct()
    {
        await _viewModel.GetActiveProduct();
        return _viewModel.ActiveProductId;
    }

    public async Task<bool> AreDigitalIOSignalsEnabled()
    {
        _areDigitalIOSignalsEnabled = await _viewModel.GetDigitalIOSignalsConfiguration();
        return _areDigitalIOSignalsEnabled;
    }

    public async Task<bool> IsExecuteEntireCycleEnabled()
    {
        _isExecuteEntireCycleEnabled = await _viewModel.GetCycleConfiguration();
        return _isExecuteEntireCycleEnabled;
    }

    public async Task<HistoryDto[]> GetHistories(HistoryFilters filters)
    {
        var histories = await _viewModel.GetFilteredHistories(filters);
        return histories.Select(x => new HistoryDto()
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = x.Product.Description,
            OperativeModeId = x.OperativeModeId,
            OperativeModeName = x.OperativeMode.Description,
            SerialNumber = x.SerialNumber,
            Created = x.Created,
            PickupTime = x.PickupTime,
            ApplicationTime = x.ApplicationTime
        })
        .OrderByDescending(x => x.Created)
        .ToArray();
    }

    public async Task DeleteAllFilteredItems(HistoryFilters filters)
    {
        await _viewModel.DeleteFilteredHistories(filters);
    }

    public async Task<LabelDto[]> GetLabels()
    {
        var labels = await _viewModel.GetLabels();
        return labels.Select(x => new LabelDto()
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductDescription = x.Product.Description,
            Content = x.Content,
        })
        .ToArray();
    }

    public async Task<ProductDto[]> GetProducts()
    {
        var products = await _viewModel.GetProducts();
        return products.Select(x => new ProductDto()
        {
            Id = x.Id,
            Description = x.Description,
        })
        .ToArray();
    }

    public async Task<RouteTypeDto[]> GetRouteTypes()
    {
        var routeTypes = await _viewModel.GetRouteTypes();
        return routeTypes.Select(x => new RouteTypeDto()
        {
            Id = x.Id,
            Description = x.Description,
        })
        .ToArray();
    }

    public async Task<RouteStepDto[]> GetRouteSteps(int productId, int routeTypeId)
    {
        var routeSteps = await _viewModel.GetRouteSteps(productId, routeTypeId);
        return routeSteps.Select(x => new RouteStepDto()
        {
            Id = x.Id,
            RouteId = x.RouteId,
            StepOrder = x.StepOrder,
            Speed = x.Speed,
            ClearZone = x.ClearZone,
            RobotPointId = x.RobotPointId,
            RobotPoint = x.RobotPoint
        })
        .OrderBy(x => x.StepOrder)
        .ToArray();
    }

    public async Task FormFeed()
    {
        _printerService.FormFeed();
    }

    public async Task GoToPosition(RobotPosition robotPosition, int routeStepId, int speed)
    {
        //////////////_robotService.ResetAlarms();
        //////////////_robotService.ClearAlarms();
        //_robotService.SetSpeedRatio(speed);
        OperationStatus status = _robotService.MoveLinearToPosition(robotPosition, speed);
        if (status == OperationStatus.OK)
        {
            await _viewModel.SaveLastExecutedRouteStep(routeStepId);
        }
    }

    public RobotPosition GetCurrentRobotPosition()
    {
        return _robotService.CurrentPosition;
    }

    public async Task UpdateStep(RouteStepDto routeStep)
    {
        if (routeStep.RobotPoint.PointType.Description == "Home")
        {
            _applicationRouteHomePosition = null;
        }

        var robotPosition = _robotService.CurrentPosition;
        await _viewModel.UpdateRobotPoint(routeStep.RobotPointId, robotPosition);
        await _viewModel.UpdateRouteStep(routeStep.Id, routeStep.Speed, routeStep.ClearZone);
        _robotService.ResetAlarms();
        _robotService.ClearAlarms();
    }
    
    public async Task UpdateStepParameters(RouteStepDto routeStep)
    {
        await _viewModel.UpdateRouteStep(routeStep.Id, routeStep.Speed, routeStep.ClearZone);
    }

    public async Task DeleteStep(RouteStepDto routeStep)
    {
        await _viewModel.DeleteRobotPoint(routeStep.RobotPointId);
        await _viewModel.DeleteRouteStep(routeStep.Id);
    }

    public void UpdateAirStatus(bool on)
    {
        _robotService.SetDigitalOutput(DigitalOutputs.AirActivation, on);
    }

    public void UpdateVacuumStatus(bool on)
    {
        _robotService.SetDigitalOutput(DigitalOutputs.VacuumActivation, on);
    }

    public async Task UpdateLabel(int labelId, string content)
    {
        await _viewModel.UpdateLabelContent(labelId, content);
    }

    public bool[] GetInputSignals()
    {
        bool[] inputSignals = new bool[8];

        for (int index = 0; index < 8; index++)
        {
            inputSignals[index] = _robotService.ReadDigitalInput(index + 1);
        }

        return inputSignals;
    }

    public bool[] GetOutputSignals()
    {
        bool[] outputSignals = new bool[8];

        for (int index = 0; index < 8; index++)
        {
            outputSignals[index] = _robotService.ReadDigitalOutput(index + 1);
        }

        return outputSignals;
    }

    public void SetDigitalOutput(int signalIndex, bool value)
    {
        _robotService.SetDigitalOutput(signalIndex, value);
    }

    public async Task ToggleAutomaticCycle(int productId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await SimulateCycle(productId);
        }
    }

    public async Task SimulateCycle(int productId)
    {
        if (_activeOperativeModeId == 0)
        {
            if (await GetActiveOperativeMode() == 0) return;
        }

        string serialNumber = Guid.NewGuid().ToString();
        _ioExternalCommunication.ResetLabelSignals();
        var status = await _cycleService.ExecutePrintCycle(productId, serialNumber, _activeOperativeModeId, string.Empty);
        _ioExternalCommunication.LabelPrinted(status);

        if (status == OperationStatus.OK)
        {
            status = await _cycleService.ExecuteApplyCycle(productId, serialNumber, _activeOperativeModeId);
            _ioExternalCommunication.LabelApplied(status);
        }
    }

    private string FormatLabel(string labelContent)
    {
        return labelContent
            .Replace("*DATA*", DateTime.Now.ToString("dd/MM/yyyy"))
            .Replace("*ORA*", DateTime.Now.ToString("HH:mm:ss"));
    }

    public void Reset()
    {
        _logger.LogInformation("Resetting...");
        IsCycling = false;
        _robotService.FullReset();
        _robotService.SetDigitalOutput(DigitalOutputs.AirActivation, false);
        _robotService.SetDigitalOutput(DigitalOutputs.VacuumActivation, false);
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

        //_robotService.SetDigitalOutput(DigitalOutputs.Ready, true);
        //    per gestione segnale di ready
        _robotService.SetDigitalOutput(DigitalOutputs.Ready, true, true);
    }

    private void ResetPrinter()
    {
    }

    public void ResetRobotAlarms()
    {
        _robotService.ResetAlarms();

    }

    public void ClearRobotAlarms()
    {
        _robotService.ClearAlarms();

    }

    public void PowerOnRobot()
    {
        _robotService.PowerOn();
    }

    public void PowerOffRobot()
    {
        _robotService.PowerOff();
    }

    public void StartDragRobot()
    {
        // resetto l'ultima posizione salvata perchè avvio il drag mode
        //_viewModel.SaveLastExecutedRouteStep(0);
        _robotService.StartDrag();
    }

    public async void StopDragRobot()
    {
        _robotService.StopDrag();
        await _viewModel.GetActiveProduct();
        int RouteStep = await _cycleService.EstimatePosition(_viewModel.ActiveProductId);
        await _viewModel.SaveLastExecutedRouteStep(RouteStep);
    }

    public void DisableRobot()
    {
        _robotService.Disable();
    }

    public void EnableRobot()
    {
        _robotService.Enable();
    }

    public bool IsRobotConnected()
    {
        return _robotService.IsConnected;
    }

    public bool IsRobotDisabled()
    {
        return _robotService.IsDisabled;
    }

    public bool IsRobotEnabled()
    {
        return _robotService.IsEnabled;
    }

    public bool IsRobotInError()
    {
        return _robotService.IsInError;
    }

    public string GetRobotStatus()
    {
        return _robotService.Status;
    }
    
    public double[] GetRobotTCPForce()
    {
        return _robotService.GetRobotTCPForce();
    }
    
    public int GetRobotSpeed()
    {
        return _robotService.GetSpeedRatio();
    }

    public async Task SaveOperativeMode(int newOperativeModeId, bool digitalIOSignalsEnabled, bool executeEntireCycleEnabled)
    {
        // Aggiorna le impostazioni nel sistema
        // Questa logica può includere la memorizzazione delle impostazioni in un database,
        // la modifica del comportamento del sistema in base alla modalità selezionata, ecc.
        if (_activeOperativeModeId != newOperativeModeId)
        {
            _activeOperativeModeId = newOperativeModeId;

            await _viewModel.SaveActiveOperativeMode(newOperativeModeId);
            _externalDeviceCommunication?.Load(_activeOperativeModeId);
            // Esempio di logica per gestire le modalità
            switch (_activeOperativeModeId)
            {
                case 1: // Manual
                    // Configurazione specifica per la modalità Manual
                    break;
                case 2: // CSE Protocol
                    // Configurazione specifica per il protocollo CSE
                    break;
                case 3: // Printer Language
                    // Configurazione specifica per la modalità Linguaggio Stampante
                    break;
            }
        }

        // Logica per utilizzare o meno gli I/O digitali

        if (_areDigitalIOSignalsEnabled != digitalIOSignalsEnabled)
        {
            _areDigitalIOSignalsEnabled = digitalIOSignalsEnabled;
            await _viewModel.SaveDigitalIOSignalsConfiguration(_areDigitalIOSignalsEnabled);
        }

        if (_isExecuteEntireCycleEnabled != executeEntireCycleEnabled)
        {
            _isExecuteEntireCycleEnabled = executeEntireCycleEnabled;
            await _viewModel.SaveCycleConfiguration(_isExecuteEntireCycleEnabled);
        }
    }

    public async Task SaveActiveProduct(int newActiveProduct)
    {
        if (_viewModel.ActiveProductId != newActiveProduct)
        {
            _viewModel.ActiveProductId = newActiveProduct;

            await _viewModel.SaveActiveProduct(newActiveProduct);

            _applicationRouteHomePosition = null;
        }
    }

    public async Task DeleteProduct(int productId)
    {
        await _viewModel.DeleteProduct(productId);
    }

    public async Task Shutdown()
    {
       await ShutdownRobot();
    }

    private async Task ShutdownRobot()
    {
        bool isRobotInDebugMode = Convert.ToBoolean(_configuration["InternalDevices:Robot:DebugMode"]);
        if (isRobotInDebugMode) return;

        _logger.LogInformation("Controller - Disconnecting Robot...");

        _robotService.Disconnect();

        string robotIpAddress = _configuration["InternalDevices:Robot:IpAddress"] ?? string.Empty;
        var ping = new Ping();

        while (ping.Send(robotIpAddress).Status == IPStatus.Success)
        {
            _logger.LogInformation("Robot ping success - Trying to turn it off with GPIO output...");
            _gpioManager.SetDigitalOutput(1, false);
            _logger.LogInformation("Robot ping success - Set Output to FALSE...");
            await Task.Delay(5000);
            _gpioManager.SetDigitalOutput(1, true);
            _logger.LogInformation("Robot ping success - Set Output to TRUE...");
            await Task.Delay(10000);
        }

        _logger.LogInformation("Robot shutdown!");
    }

    public void SetDobotSpeed(int speed)
    {
       _robotService.SetSpeedRatio(speed);
    }

    private async Task CheckHomePosition()
    {
        while (true)
        {
            if (_viewModel.ActiveProductId == 0)
            {
                _viewModel.ActiveProductId = await _viewModel.GetActiveProduct();
            }

            if (_applicationRouteHomePosition is null)
            {
                _applicationRouteHomePosition = await _viewModel.GetApplicationRouteHomePosition(_viewModel.ActiveProductId);
            }

            bool isNearHome = RobotPosition.AreNear(_robotService.CurrentPosition, _applicationRouteHomePosition, 0.1f);
            if (isNearHome)
            {
                if (!_robotService.ReadDigitalOutput(DigitalOutputs.Home))
                {
                    _robotService.SetDigitalOutput(DigitalOutputs.Home, true);
                }
            }
            else
            {
                if (_robotService.ReadDigitalOutput(DigitalOutputs.Home))
                {
                    _robotService.SetDigitalOutput(DigitalOutputs.Home, false);
                }
            }

            double[] TForce = _robotService.GetRobotTCPForce();
            //const int RobotAxis = 6;

            //for (int index = 0; index < RobotAxis; index++)
            //{
            //    Debug.WriteLine("DOBOT_Current{0}: {1}", index, TForce[index]);
            //}

            await Task.Delay(1000);
        }
    }
}
