using Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.CodeDom;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.Serialization;
using UseCases.core;
using BackEnds.RoboPrinter.Models;
using BackEnds.RoboPrinter.RobotModels;

namespace BackEnds.RoboPrinter.Services.IServices;

public class CycleService : ICycleService
{
    private readonly ILogger<CycleService> _logger;
    private readonly IRobotService _robotService;
    private readonly IPrinterService _printerService;
    private readonly ViewModel _viewModel;
    private int _activeOperativeModeId = 0;
    private CancellationTokenSource _cts = new();

    public bool IsCycling { get; private set; } = false;

    public CycleService(ILogger<CycleService> logger, IRobotService robotService, IPrinterService printerService, ViewModel viewModel)
    {
        _logger = logger;
        _robotService = robotService;
        _printerService = printerService;
        _viewModel = viewModel;
    }

    public async Task<OperationStatus> ExecutePrintCycle(int productId, string serialNumber, int activeOperativeMode, string labelContent)
    {
        if (IsCycling)
        {
            _logger.LogInformation("ExecutePrintCycle called while is already cycling!");
            return OperationStatus.Cycling;
        }

        _cts.Cancel();
        _logger.LogInformation("ExecutePrintCycle starting...");
        IsCycling = true;

        /* Questo nuovo ciclo viene utilizzato per:
         * - mandare il robot in posizione di prelievo con aria accesa;
         * - stampare etichetta;
         * - far completare al robot il percorso di prelievo per tornare in home;
         * - mandare il robot ad applicare l'etichetta e tornare in home;
        */

        var pickupRouteHomePosition = await _viewModel.GetPickupRouteHomePosition(productId);
        
        bool homeStatus = await CheckHomePosition(pickupRouteHomePosition);

        OperationStatus status = OperationStatus.Cycling;

        if (homeStatus)
        { 
            _cts = new CancellationTokenSource();

            var printerStatus = _printerService.GetStatus();

            if (printerStatus == PrinterStatus.LabelPendingOnSensor)
            {
                ResetCycle();
                return OperationStatus.LabelAlreadyPrinted;
            }
            else if (printerStatus == PrinterStatus.PaperLow || printerStatus == PrinterStatus.RibbonLow)
            {
                _logger.LogInformation("Printer status is: {Printerstatus} - Raising Digital Output signal...", printerStatus);
                _robotService.SetDigitalOutput(DigitalOutputs.PrinterLowMaterial, true);
            }
            else if (printerStatus == PrinterStatus.OK)
            {
                _robotService.SetDigitalOutput(DigitalOutputs.PrinterLowMaterial, false);
            }

            await _viewModel.AddNewHistory(productId, serialNumber, activeOperativeMode);

            status = await PrepareForLabelPick(productId);

            if (status != OperationStatus.OK)
            {
                ResetCycle();
                return status;
            }

            status = await PrintAndPickLabel(productId, labelContent);

            if (status != OperationStatus.OK)
            {
                ResetCycle();
                return status;
            }

            status = await ReturnToPickHomePosition(productId);

            if (status != OperationStatus.OK)
            {
                ResetCycle();
                return status;
            }

            await _viewModel.UpdatePickupTime(productId, serialNumber);

            _logger.LogInformation("SimulateCycle completed!");
        }
        else
        {
            status = OperationStatus.RobotPositionError;
        }

        /* Questo vecchio ciclo veniva utilizzato per:
         * - stampare etichetta;
         * - mandare il robot a prelevare l'etichetta e tornare in home;
         * - mandare il robot ad applicare l'etichetta e tornare in home;
        */
        //await PrintLabel(productId);
        //await PickLabel(productId);
        //await ApplyLabel(productId);
        IsCycling = false;
        return status;
    }

    public async Task<OperationStatus> ExecuteApplyCycle(int productId, string serialNumber, int activeOperativeMode)
    {
        if (IsCycling)
        {
            _logger.LogInformation("ExecuteApplyCycle called while is already cycling!");
            return OperationStatus.Cycling;
        }

        _cts.Cancel();
        //_robotService.SetSpeedRatio(100);
        _logger.LogInformation("ExecuteApplyCycle starting...");
        IsCycling = true;

        /* Questo nuovo ciclo viene utilizzato per:
         * - mandare il robot in posizione di prelievo con aria accesa;
         * - stampare etichetta;
         * - far completare al robot il percorso di prelievo per tornare in home;
        */

        if (!_robotService.ReadDigitalInput(DigitalInputs.Presence))
        {
            _logger.LogInformation("ExecuteApplyCycle - Presence Input Signal is off!");
            ResetCycle();
            return OperationStatus.LabelNotPicked;
        }

        var applicationRouteHomePosition = await _viewModel.GetApplicationRouteHomePosition(productId);
        _cts = new CancellationTokenSource();

        bool homeStatus = await CheckHomePosition(applicationRouteHomePosition);

        var status = await ApplyLabel(productId);

        if (status != OperationStatus.OK)
        {
            ResetCycle();
            return status;
        }

        await _viewModel.UpdateApplicationTime(productId, serialNumber);

        /* Questo vecchio ciclo veniva utilizzato per:
         * - stampare etichetta;
         * - mandare il robot a prelevare l'etichetta e tornare in home;
         * - mandare il robot ad applicare l'etichetta e tornare in home;
        */
        //await PrintLabel(productId);
        //await PickLabel(productId);
        //await ApplyLabel(productId);
        IsCycling = false;
        _logger.LogInformation("ExecuteApplyCycle completed!");
        return status;
    }

    private async Task<OperationStatus> PrepareForLabelPick(int productId)
    {
        _logger.LogInformation("PrepareForLabelPick starting on product: {ProductId}", productId);
        var stopwatch = Stopwatch.StartNew();
        OperationStatus status = OperationStatus.Error;

        var pickupRoute = await _viewModel.GetRobotPickupRoute(productId);
        if (pickupRoute is null)
        {
            status = OperationStatus.ProductNotFound;
        }
        else
        {
            var pickPosition = pickupRoute.FirstOrDefault(p => p.RobotPoint.PointType.Description.Contains("Pickup"));

            if (pickPosition is not null)
            {
                foreach (var step in pickupRoute.Where(p => p.StepOrder <= pickPosition.StepOrder))
                {
                    //_robotService.SetSpeedRatio(step.Speed);
                    status = MoveToPosition(step);

                    if (status != OperationStatus.OK)
                    {
                        _logger.LogInformation("PrepareForLabelPick - Failed MoveToPosition");
                        return status;
                    }

                    _robotService.SetDigitalOutput(DigitalOutputs.Safe, step.ClearZone);
                    await _viewModel.SaveLastExecutedRouteStep(step.Id);
                }

                //_currentRouteId = pickPosition.RouteId;
                //await _viewModel.SaveLastExecutedRoute(pickPosition.RouteId);
                //MoveToPositions(pickupRoute.Where(p => p.StepOrder <= pickPosition.StepOrder).ToArray());
            }

            //_robotService.SetDigitalOutput(DigitalOutputs.VacuumActivation, true);
        }

        stopwatch.Stop();
        _logger.LogInformation("PrepareForLabelPick completed with status: {Status} on product: {ProductId} in {Duration}ms",
            status, productId, stopwatch.ElapsedMilliseconds);
        return status;
    }

    private async Task<OperationStatus> ReturnToPickHomePosition(int productId)
    {
        _logger.LogInformation("ReturnToPickHomePosition starting on product: {ProductId}", productId);

        var stopwatch = Stopwatch.StartNew();
        OperationStatus status = OperationStatus.Error;

        var pickupRoute = await _viewModel.GetRobotPickupRoute(productId);

        if (pickupRoute is null)
        {
            status = OperationStatus.ProductNotFound;
        }
        else
        {
            var chrono = Stopwatch.StartNew();

            while (!_robotService.ReadDigitalInput(DigitalInputs.Presence) && chrono.ElapsedMilliseconds < 5 * 1000)
            {
                await Task.Delay(50);
            }

            _robotService.SetDigitalOutput(DigitalOutputs.AirActivation, false);

            if (chrono.ElapsedMilliseconds > 5 * 1000)
            {
                status = OperationStatus.LabelNotPicked;
            }
            else
            {
                status = OperationStatus.OK;
                var pickPosition = pickupRoute.FirstOrDefault(p => p.RobotPoint.PointType.Description.Contains("Pickup"));

                if (pickPosition is not null)
                {
                    foreach (var step in pickupRoute.Where(p => p.StepOrder > pickPosition.StepOrder))
                    {
                        //_robotService.SetSpeedRatio(step.Speed);
                        status = MoveToPosition(step);

                        if (status != OperationStatus.OK)
                        {
                            _logger.LogInformation("ReturnToPickHomePosition - Failed MoveToPosition");
                            return status;
                        }
                        _robotService.SetDigitalOutput(DigitalOutputs.Safe, step.ClearZone);
                        await _viewModel.SaveLastExecutedRouteStep(step.Id);
                    }
                }
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("ReturnToPickHomePosition completed with status: {Status} on product: {ProductId} in {Duration}ms",
            status, productId, stopwatch.ElapsedMilliseconds);
        return status;
    }

    //private async Task<OperationStatus> PickLabel(int productId)
    //{
    //    _logger.LogInformation("PickLabel starting on product: {ProductId}", productId);

    //    var stopwatch = Stopwatch.StartNew();
    //    OperationStatus status = OperationStatus.Error;

    //    var pickupRoute = await _viewModel.GetRobotPickupRoute(productId);

    //    if (pickupRoute is null)
    //    {
    //        status = OperationStatus.ProductNotFound;
    //    }
    //    else
    //    {
    //        status = _printerService.Print();

    //        var pickPosition = pickupRoute.FirstOrDefault(p => p.RobotPoint.PointType.Description == "Pickup Point");
    //        MoveToPositions(pickupRoute.Where(p => p.StepOrder <= pickPosition.StepOrder).ToArray());

    //        //_robotService.SetDigitalOutput(DigitalOutputs.VacuumActivation, true);

    //        //soffiatore
    //        //_robotService.SetDigitalOutput(DigitalOutputs.HomePosition, false);

    //        var chrono = Stopwatch.StartNew();

    //        while (!_robotService.ReadDigitalInput(DigitalInputs.Presence) && chrono.ElapsedMilliseconds < 5 * 1000)
    //        {
    //            await Task.Delay(50);
    //        }

    //        if (chrono.ElapsedMilliseconds > 5 * 1000)
    //        {
    //            status = OperationStatus.LabelNotPicked;
    //        }
    //        else
    //        {
    //            status = OperationStatus.OK;
    //        }

    //        MoveToPositions(pickupRoute.Where(p => p.StepOrder > pickPosition.StepOrder).ToArray());
    //    }

    //    stopwatch.Stop();
    //    _logger.LogInformation("PickLabel completed with status: {Status} on product: {ProductId} in {Duration}ms",
    //        status, productId, stopwatch.ElapsedMilliseconds);
    //    return status;
    //}

    private async Task<OperationStatus> PrintAndPickLabel(int productId, string labelContent)
    {
        _logger.LogInformation("PrintLabel starting on product: {ProductId}", productId);

        var stopwatch = Stopwatch.StartNew();
        OperationStatus status = OperationStatus.Error;
        _robotService.SetDigitalOutput(DigitalOutputs.AirActivation, true);
        

        if (string.IsNullOrEmpty(labelContent))
        {
            status = await PrintProductLabel(productId);
        }
        else
        {
            status = await PrintMessageLabel(labelContent);
        }

        _robotService.SetDigitalOutput(DigitalOutputs.VacuumActivation, true);
        //_robotService.SetDigitalOutput(DigitalOutputs.AirActivation, false);

        stopwatch.Stop();
        _logger.LogInformation("PrintLabel completed with status: {Status} on product: {ProductId} in {Duration}ms",
            status, productId, stopwatch.ElapsedMilliseconds);
        return status;
    }

    private async Task<OperationStatus> PrintProductLabel(int productId)
    {
        var labelContent = await _viewModel.GetLabelContent(productId);
        if (string.IsNullOrEmpty(labelContent))
        {
            return OperationStatus.ProductNotFound;
        }

        string formattedLabel = FormatLabel(labelContent);

        var parameters = new Dictionary<string, string>
            {
                { "LABEL", formattedLabel }
            };

        OperationStatus status = _printerService.Load(parameters);
        status = _printerService.Print();
        return status;
    }

    private async Task<OperationStatus> PrintMessageLabel(string message)
    {
        var status = _printerService.Send(message);
        return status;
    }

    private async Task<OperationStatus> ApplyLabel(int productId)
    {
        _logger.LogInformation("ApplyLabel starting on product: {ProductId}", productId);
        var stopwatch = Stopwatch.StartNew();
        OperationStatus status = OperationStatus.Error;
        var applicationRoute = await _viewModel.GetRobotApplicationRoute(productId);

        if (applicationRoute is null)
        {
            status = OperationStatus.ProductNotFound;
        }
        else
        {
            var applyPosition = applicationRoute.FirstOrDefault(p => p.RobotPoint.PointType.Description == "Application Point");

            if (applyPosition is not null)
            {
                foreach (var step in applicationRoute.Where(p => p.StepOrder <= applyPosition.StepOrder))
                {
                    //_robotService.SetSpeedRatio(step.Speed);
                    status = MoveToPosition(step);

                    if (status != OperationStatus.OK)
                    {
                        _logger.LogInformation("ApplyLabel - Failed MoveToPosition");
                        return status;
                    }
                    _robotService.SetDigitalOutput(DigitalOutputs.Safe, step.ClearZone);
                    await _viewModel.SaveLastExecutedRouteStep(step.Id);
                }
            }

            _robotService.SetDigitalOutput(DigitalOutputs.VacuumActivation, false);
            

            var chrono = Stopwatch.StartNew();

            while (_robotService.ReadDigitalInput(DigitalInputs.Presence) && chrono.ElapsedMilliseconds < 5 * 1000)
            {
                await Task.Delay(50);
            }

            if (chrono.ElapsedMilliseconds > 5 * 1000)
            {
                status = OperationStatus.LabelNotApplied;
            }
            else
            {
                status = OperationStatus.OK;
            }

            foreach (var step in applicationRoute.Where(p => p.StepOrder > applyPosition.StepOrder))
            {
                //_robotService.SetSpeedRatio(step.Speed);
                status = MoveToPosition(step);

                if (status != OperationStatus.OK)
                {
                    _logger.LogInformation("ApplyLabel - Failed MoveToPosition");
                    return status;
                }

                _robotService.SetDigitalOutput(DigitalOutputs.Safe, step.ClearZone);
                await _viewModel.SaveLastExecutedRouteStep(step.Id);
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("ApplyLabel completed with status: {Status} on product: {ProductId} in {Duration}ms",
            status, productId, stopwatch.ElapsedMilliseconds);
        return status;
    }

    private OperationStatus MoveToPosition(RouteStep step)
    {
        var robotPosition = new RobotPosition()
        {
            X = step.RobotPoint.X,
            Y = step.RobotPoint.Y,
            Z = step.RobotPoint.Z,
            Yaw = step.RobotPoint.Yaw,
            Pitch = step.RobotPoint.Pitch,
            Roll = step.RobotPoint.Roll
        };

        return _robotService.MoveLinearToPosition(robotPosition, step.Speed);
    }

    private void MoveToPositions(RouteStep[] pickPositions)
    {
        if (pickPositions.Length == 0) return;

        var robotPositions = pickPositions
            .Select(rp => new RobotPosition()
            {
                X = rp.RobotPoint.X,
                Y = rp.RobotPoint.Y,
                Z = rp.RobotPoint.Z,
                Yaw = rp.RobotPoint.Yaw,
                Pitch = rp.RobotPoint.Pitch,
                Roll = rp.RobotPoint.Roll
            })
            .ToArray();

        _robotService.MoveToMultiplePositions(robotPositions);
    }

    private string FormatLabel(string labelContent)
    {
        return labelContent
            .Replace("*DATA*", DateTime.Now.ToString("dd/MM/yyyy"))
            .Replace("*ORA*", DateTime.Now.ToString("HH:mm:ss"));
    }

    public async Task ReturnToHome()
    {
        if (_robotService.Status != "Enabled") return;

        int lastRouteStepId = await _viewModel.GetLastExecutedRouteStep();

        if (lastRouteStepId == 0)
        {
            _logger.LogInformation("ReturnToHomePosition - Last Route Step Id is 0");
            return;
        }

        _logger.LogInformation("ReturnToHomePosition - Getting route from RouteStepId {RouteStep}", lastRouteStepId);
        int? routeId = await _viewModel.GetRouteIdByRouteStepId(lastRouteStepId);

        if (routeId is null || routeId == 0)
        {
            _logger.LogInformation("ReturnToHomePosition - Last Route Step Id has no route");
            return;
        }

        var routeSteps = await _viewModel.GetRobotRoute(routeId.Value);
        //int currentStepOrder = 0;
        //var minPointsDistance = double.MaxValue;

        //foreach (var routeStep in route)
        //{
        //    var robotPosition = new RobotPosition
        //    {
        //        X = routeStep.RobotPoint.X,
        //        Y = routeStep.RobotPoint.Y,
        //        Z = routeStep.RobotPoint.Z,
        //        Yaw = routeStep.RobotPoint.Yaw,
        //        Pitch = routeStep.RobotPoint.Pitch,
        //        Roll = routeStep.RobotPoint.Roll,
        //    };
        //    double distance = RobotPosition.CalculatePointsDistance(_robotService.CurrentPosition, robotPosition);

        //    if (distance < minPointsDistance)
        //    {
        //        minPointsDistance = distance;
        //        currentStepOrder = routeStep.StepOrder;
        //    }
        //}

        var homeStep = routeSteps.FirstOrDefault(x => x.RobotPoint.PointType.Description == "Home");
        var lastStep = routeSteps.FirstOrDefault(x => x.Id == lastRouteStepId);

        if (lastStep is null || homeStep is null)
        {
            _logger.LogError("ReturnToHomePosition - No Valid RouteSteps found with id {0}", lastRouteStepId);
            return;
        }

        if (homeStep.StepOrder > lastStep.StepOrder)
        {
            var routeStepsBackward = routeSteps
                .Where(x => x.StepOrder >= lastStep.StepOrder &&
                            !x.RobotPoint.PointType.Description.Contains("Pickup") && 
                            !x.RobotPoint.PointType.Description.Contains("Application"))
                .OrderBy(x => x.StepOrder);

            foreach (var step in routeStepsBackward)
            {
                //_robotService.SetSpeedRatio(step.Speed);

                OperationStatus status = MoveToPosition(step);

                // verifico che il movimento sia andato a buon fine
                if (status != OperationStatus.OK)
                {
                    _logger.LogInformation("ReturnToHome - Failed MoveToPosition");
                    return;
                }
                _robotService.SetDigitalOutput(DigitalOutputs.Safe, step.ClearZone);
                await _viewModel.SaveLastExecutedRouteStep(step.Id);
            }
        }
        else if (homeStep.StepOrder < lastStep.StepOrder)
        {
            var routeStepsForward = routeSteps
                .Where(x => x.StepOrder <= lastStep.StepOrder &&
                            !x.RobotPoint.PointType.Description.Contains("Pickup") &&
                            !x.RobotPoint.PointType.Description.Contains("Application"))
                .OrderByDescending(x => x.StepOrder);

            foreach (var step in routeStepsForward)
            {
                //_robotService.SetSpeedRatio(step.Speed);
                MoveToPosition(step);
                _robotService.SetDigitalOutput(DigitalOutputs.Safe, step.ClearZone);
                await _viewModel.SaveLastExecutedRouteStep(step.Id);
            }            
        }
        else
        {
            //_robotService.SetSpeedRatio(lastStep.Speed);
            MoveToPosition(lastStep);
            _robotService.SetDigitalOutput(DigitalOutputs.Safe, lastStep.ClearZone);
        }

        /////////////////////////////////////////////////////////////_robotService.SetDigitalOutput(DigitalOutputs.Home, true);
    }

    private void ResetCycle()
    {
        _robotService.SetDigitalOutput(DigitalOutputs.AirActivation, false); 
        _robotService.SetDigitalOutput(DigitalOutputs.VacuumActivation, false);
        // aggiunto
        //    per gestione segnale di ready   _robotService.SetDigitalOutput(DigitalOutputs.Ready, false, false);

        IsCycling = false;
    }

    private async Task<bool> CheckHomePosition(RobotPosition rp)
    {
        //while (!_cts.IsCancellationRequested)
        //{
        bool isNearHome = RobotPosition.AreNear(_robotService.CurrentPosition, rp, 0.1f);
        return isNearHome;
            //if(isNearHome && !_robotService.ReadDigitalOutput(DigitalOutputs.Home))
            //{
            //    _robotService.SetDigitalOutput(DigitalOutputs.Home, true);
            //}
            //await Task.Delay(100);
            //}
    }

    public async Task<int> EstimatePosition(int productId)
    {
        int currentStepOrder = 0;
        var minPointsDistance = double.MaxValue;
        var route = await _viewModel.GetRouteStepsByProduct(productId);

        foreach (var routeStep in route)
        {
            var robotPosition = new RobotPosition
            {
                X = routeStep.RobotPoint.X,
                Y = routeStep.RobotPoint.Y,
                Z = routeStep.RobotPoint.Z,
                Yaw = routeStep.RobotPoint.Yaw,
                Pitch = routeStep.RobotPoint.Pitch,
                Roll = routeStep.RobotPoint.Roll,
            };
            double distance = RobotPosition.CalculatePointsDistance(_robotService.CurrentPosition, robotPosition);

            if (distance < minPointsDistance)
            {
                minPointsDistance = distance;
                currentStepOrder = routeStep.RobotPointId;
            }
        }
        return currentStepOrder;
    }
}
