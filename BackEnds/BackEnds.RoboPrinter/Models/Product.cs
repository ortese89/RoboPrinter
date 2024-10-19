using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class Product
{
    public Product()
    {
        Histories = [];
    }

    [Key]
    public int Id { get; set; }
    public string Description { get; set; }
    public ICollection<History> Histories { get; set; }
}
