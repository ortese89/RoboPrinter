using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class History
{
    [Key]
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int OperativeModeId { get; set; }
    public OperativeMode OperativeMode { get; set; }
    public string SerialNumber { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? ApplicationTime { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
}
