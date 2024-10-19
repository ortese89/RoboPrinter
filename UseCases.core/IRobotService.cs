using Entities;

namespace UseCases.core;

public interface IRobotService
{
    enum JogMovement
    {
        X, 
        Y, 
        Z,
        Yaw, 
        Pitch, 
        Roll,
        J1,
        J2,
        J3,
        J4,
        J5,
        J6
    };
    bool IsConnected { get; }
    bool IsEnabled { get; }
    bool IsDisabled { get; }
    bool IsInError { get; }
    string Status { get; }
    RobotPosition CurrentPosition { get; }
    OperationStatus Load(Dictionary<string, string> parameters);
    OperationStatus MoveLinearToPosition(RobotPosition robotPosition);
    OperationStatus MoveLinearToPosition(RobotPosition robotPosition, int speed);
    OperationStatus MoveToMultiplePositions(RobotPosition[] robotPositions);
    OperationStatus MoveJointToPosition(RobotPosition robotPosition);
    void ClearAlarms();
    void Connect(string ipAddress, int port, bool debugMode);
    void Disable();
    void Disconnect();
    void Enable();
    void FullReset();
    double[] GetRobotTCPForce();
    int GetSpeedRatio();
    void MoveJog(JogMovement jogMovement, bool forward);
    void PowerOn();
    void PowerOff();
    bool ReadDigitalInput(int index);
    bool ReadDigitalOutput(int index);
    void ResetAlarms();
    void SetDigitalOutput(int index, bool value);
    Task SetDigitalOutput(int index, bool value, bool swap);
    void SetSpeedRatio(int value);
    void StartDrag();
    void StopDrag();
    void StopJog();
}
