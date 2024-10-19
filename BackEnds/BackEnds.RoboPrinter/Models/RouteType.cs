using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class RouteType
{
    public RouteType()
    {
        Routes = [];
    }
    [Key]
    public int Id { get; set; }
    public string Description { get; set; }
    public ICollection<Route> Routes { get; set; }
}
