using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class RouteStep
{
    [Key]
    public int Id { get; set; }
    public int RouteId { get; set; }
    public Route Route { get; set; }
    public int RobotPointId { get; set; }
    public RobotPoint RobotPoint { get; set; }
    public int StepOrder { get; set; }
    public int Speed { get; set; }
    public bool ClearZone { get; set; }
}
