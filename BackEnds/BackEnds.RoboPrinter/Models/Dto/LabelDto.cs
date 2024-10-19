namespace BackEnds.RoboPrinter.Models.Dto;

public class LabelDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public string ProductDescription { get; set; }
    public string Content { get; set; } 
}