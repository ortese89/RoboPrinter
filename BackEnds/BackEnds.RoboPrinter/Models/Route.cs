using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class Route
{
    public Route()
    {
        RouteSteps = [];
    }

    [Key]
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int RouteTypeId { get; set; }
    public RouteType RouteType { get; set; }
    public ICollection<RouteStep> RouteSteps { get; set; }
}
