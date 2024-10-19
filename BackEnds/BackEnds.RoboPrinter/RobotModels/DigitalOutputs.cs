namespace BackEnds.RoboPrinter.RobotModels;

public static class DigitalOutputs
{
    public static int LabelPrinted { get; } = 1;
    public static int LabelApplied { get; } = 2;
    public static int Home { get; } = 3;
    public static int Ready { get; } = 4;
    public static int PrinterLowMaterial { get; } = 5;
    public static int Safe { get; } = 6;
    public static int AirActivation { get; } = 7;
    public static int VacuumActivation { get; } = 8;
}
