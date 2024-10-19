namespace BackEnds.RoboPrinter.Models.Dto;

public class RouteStepDto
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public int StepOrder { get; set; }
    public int Speed { get; set; }
    public bool ClearZone { get; set; }
    public int RobotPointId { get; set; }
    public RobotPoint RobotPoint { get; set; }
}