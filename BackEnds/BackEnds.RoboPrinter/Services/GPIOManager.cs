using System.Runtime.InteropServices;

namespace BackEnds.RoboPrinter.Services;

public class GPIOManager
{
    [DllImport("WinIo64.dll")]
    public static extern bool InitializeWinIo();

    [DllImport("WinIo64.dll")]
    public static extern void ShutdownWinIo();

    [DllImport("WinIo64.dll")]
    public static extern bool GetPortVal(ushort portAddr, out uint portVal, ushort size);

    [DllImport("WinIo64.dll")]
    public static extern bool SetPortVal(ushort portAddr, uint portVal, ushort size);

    private const ushort PORT_ADDRESS = 0xA06;
    private readonly bool _isWinIoInitialized;

    public GPIOManager()
    {
        _isWinIoInitialized = InitializeWinIo();
    }

    public void SetDigitalOutput(int index, bool value)
    {
        if (!_isWinIoInitialized) return;
        int bitPosition = index - 1;
        uint mask = (uint)(1 << bitPosition);

        GetPortVal(PORT_ADDRESS, out uint portValue, 1);

        if (value)
        {
            portValue |= mask;
        }
        else
        {
            portValue &= ~mask;
        }

        SetPortVal(PORT_ADDRESS, portValue, 1);
    }

    public bool ReadDigitalOutput(int index)
    {
        if (!_isWinIoInitialized) return false;
        int bitPosition = index - 1;
        uint mask = (uint)(1 << bitPosition);

        GetPortVal(PORT_ADDRESS, out uint portValue, 1);
        return (portValue & mask) != 0;
    }

    public bool ReadDigitalInput(int index)
    {
        if (!_isWinIoInitialized) return false;
        int bitPosition = index - 1;
        uint mask = (uint)(1 << bitPosition);

        GetPortVal(PORT_ADDRESS, out uint portValue, 1);
        return (portValue & mask) != 0;
    }
}

