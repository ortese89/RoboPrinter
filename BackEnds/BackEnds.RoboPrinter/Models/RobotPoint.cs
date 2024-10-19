using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class RobotPoint
{
    public RobotPoint()
    {
        RouteSteps = [];
    }

    [Key]
    public int Id { get; set; }
    public int PointTypeId { get; set; }
    public PointType PointType { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Roll { get; set; }
    public ICollection<RouteStep> RouteSteps { get; set; }
}
