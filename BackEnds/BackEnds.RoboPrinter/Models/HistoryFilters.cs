namespace BackEnds.RoboPrinter.Models;

public class HistoryFilters
{
    public int StartIndex { get; set; }
    public int Size { get; set; }
    public int ProductId { get; set; }
    public int OperativeModeId { get; set; }
    public string SerialNumber { get; set; }
    public DateTime? PickupTimeFrom { get; set; }
    public DateTime? PickupTimeTo { get; set; }
    public DateTime? ApplicationTimeFrom { get; set; }
    public DateTime? ApplicationTimeTo { get; set; }

    public HistoryFilters(int startIndex, int size)
    {
        StartIndex = startIndex;
        Size = size;
    }


    public HistoryFilters(int startIndex)
    {
        StartIndex = startIndex;
        Size = 999999;
    }
}