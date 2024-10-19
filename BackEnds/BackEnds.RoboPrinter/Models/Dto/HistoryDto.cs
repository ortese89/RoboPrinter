using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models.Dto;

public class HistoryDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int OperativeModeId { get; set; }
    public string OperativeModeName { get; set; }
    public string SerialNumber { get; set; }
    public DateTime Created { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? ApplicationTime { get; set; }
}