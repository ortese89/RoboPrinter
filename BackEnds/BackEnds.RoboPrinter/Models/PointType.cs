using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class PointType
{
    public PointType()
    {
        RobotPoints = [];
    }
    [Key]
    public int Id { get; set; }
    public string Description { get; set; }
    public ICollection<RobotPoint> RobotPoints { get; set; }

}
