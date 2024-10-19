using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class Label
{
    [Key]
    public int Id { get; set; }
    public int ProductId {  get; set; }
    public Product Product { get; set; }    
    public string Content { get; set; }
}
