namespace BackEnds.RoboPrinter;

internal class SD
{
    internal object? ConvertValue(string value, int dataTypeId)
    {
        return (int)dataTypeId switch
        {
            1 => int.TryParse(value, out int intResult) ? intResult : (object)null,
            2 => double.TryParse(value, out double doubleResult) ? doubleResult : (object)null,
            3 => value,
            4 => bool.TryParse(value, out bool boolResult) ? boolResult : (object)null,
            _ => value,
        };
    }

    internal enum OperativeModes {
        Manual = 1,
        CSEProtocol = 2,
        PrinterLanguage = 3
    }
}
