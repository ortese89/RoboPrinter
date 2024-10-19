namespace UseCases.core;

public interface IDigitalSignalService
{
    public void SetDigitalOutput(int index, bool value);
    public bool ReadDigitalInput(int index);
    public bool ReadDigitalOutput(int index);
}
