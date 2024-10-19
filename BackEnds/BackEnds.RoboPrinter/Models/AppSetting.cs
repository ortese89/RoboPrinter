using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class AppSetting
{
    [Key]
    public int Id { get; set; }
    public string Description { get; set; }
    public string Value { get; set; }
    public int DataTypeId { get; set; }
    public DataType DataType { get; set; } 
}
