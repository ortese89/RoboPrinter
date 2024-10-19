using System.ComponentModel.DataAnnotations;

namespace BackEnds.RoboPrinter.Models;

public class DataType
{
    public DataType()
    {
        Configurations = [];
    }

    [Key]
    public int Id { get; set; }
    public string Description { get; set; }
    public ICollection<AppSetting> Configurations { get; set; }
}
