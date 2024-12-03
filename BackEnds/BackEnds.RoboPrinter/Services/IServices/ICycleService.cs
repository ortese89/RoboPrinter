using Entities;

namespace BackEnds.RoboPrinter.Services.IServices;

public interface ICycleService

{
    bool IsCycling { get; set; }

    Task<OperationStatus> ExecutePrintCycle(int productId, string serialNumber, int activeOperativeMode, string labelContent);
    Task<OperationStatus> ExecuteApplyCycle(int productId, string serialNumber, int activeOperativeMode);
    Task<int> EstimatePosition(int productId);
    Task ReturnToHome();
}
