using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class OperativeMode
{
    public OperativeMode()
    {
        Histories = [];
    }
    [Key]
    public int Id { get; set; }
    public string Description { get; set; }
    public ICollection<History> Histories { get; set; }

}
