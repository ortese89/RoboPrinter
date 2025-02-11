using Entities;
using System.Diagnostics;
using System.Globalization;
using UseCases.core;
using static UseCases.core.IRobotService;
using System.Timers;
using log4net.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RobotAdapters.DobotAdapter;

public class Dobot : IRobotService
{
    private string _ipAddress = string.Empty;
    private int _port;
    private bool _debugMode = false;
    private bool _isConnected = false;
    private bool _IsEnabled = false;
    private bool _IsDisabled = false;
    private bool _IsInError = false;
    private string _errorCode = "";
    private const int TimeoutMovement = 30000;
    private const int FeedbackPort = 30004;
    private const int MovePort = 30003;
    private const int DashboardPort = 29999;
    private Feedback _feedback = new();
    private DobotMove _dobotMove = new();
    private Dashboard _dashboard = new();
    private float _movementTolerance = 0f;
    private System.Timers.Timer _timer;
    private long _robotMode = -1;
    private const string NoErrorCode = "0,{[\n\t[],\n\t[],\n\t[],\n\t[],\n\t[],\n\t[],\n\t[]\n]\n},GetErrorID();";
    string elbowSingularityCode = ((int)ErrorCodes.ElbowSingularity).ToString();
    string emergencyStopPressedCode = ((int)ErrorCodes.EmergencyStopPressed).ToString();
    string emergencyStopTriggeredCode = ((int)ErrorCodes.EmergencyStopTriggered).ToString();
    string abnormalEmergencyStopCode = ((int)ErrorCodes.AbnormalEmergencyStop).ToString();

    private enum ErrorCodes
    {
        ElbowSingularity =36,
        EmergencyStopPressed = 116,
        EmergencyStopTriggered = 165,
        AbnormalEmergencyStop = 166
    }

    private enum States
    {
        Disconnected = -1,
        Init = 1,
        BrakeOpen = 2,
        PowerOff = 3,
        Disabled = 4,
        Enabled = 5,
        DragMode = 6,
        Running = 7,
        Recording = 8,
        Error = 9,
        Pause = 10,
        Jog = 11,
    }

    public Dobot()
    {
        // Inizializza il timer
        _timer = new System.Timers.Timer(500); // 500 ms
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _robotMode = _feedback.FeedbackData.RobotMode;
    }

    public void StopTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }

    public bool IsConnected {
        get {
            //return _isConnected;
            return _dashboard.IsConnected();
        }
    }
    public bool IsEnabled
    {
        get
        {
            _IsEnabled = _feedback.IsEnabled();
            return _IsEnabled;
        }

    }
    public bool IsDisabled
    {
        get
        {
            _IsDisabled = _feedback.IsDisabled();
            return _IsDisabled;
        }
    }
    public bool IsInError
    {
        get
        {
            _IsInError = _feedback.IsInError();
            return _IsInError;
        }
    }
    public string Status
    {
        get
        {
            return ((States)_robotMode).ToString();
        }
    }

    public string ErrorCode
    {
        get
        {
            return _errorCode;
        }
    }
    public RobotPosition CurrentPosition
    {
        get
        {
            float[] feedbackData = _feedback.FeedbackData.ToolVectorActual.Select(d => (float)d).ToArray();
            return new RobotPosition(feedbackData[0], feedbackData[1], feedbackData[2], feedbackData[3], feedbackData[4], feedbackData[5]);
        }
    }

    public OperationStatus Load(Dictionary<string, string> parameters)
    {
        if (_debugMode) return OperationStatus.OK;

        if (parameters.ContainsKey("MOVEMENTTOLERANCE"))
        {
            if (float.TryParse(parameters["MOVEMENTTOLERANCE"], CultureInfo.InvariantCulture, out float movementTolerance))
            {
                _movementTolerance = movementTolerance;
            }
        }

        return OperationStatus.OK;
    }

    public OperationStatus MoveJointToPosition(RobotPosition robotPosition)
    {
        if (_debugMode) return OperationStatus.OK;

        //if (!_feedback.IsEnabled())
        //{
        //    _dashboard.EnableRobot();
        //}

        if ((States)_robotMode != States.Enabled && (States)_robotMode != States.Running) return OperationStatus.RobotMovementFailed;

        _dobotMove.MovJ(robotPosition);
        var chrono = Stopwatch.StartNew();

        while (!RobotPosition.AreEqual(CurrentPosition, robotPosition) && chrono.ElapsedMilliseconds < TimeoutMovement)
        {
            Thread.Sleep(50);
        }

        return chrono.ElapsedMilliseconds > TimeoutMovement ? OperationStatus.OK : OperationStatus.RobotMovementFailed;
    }

    public OperationStatus MoveLinearToPosition(RobotPosition robotPosition)
    {
        if (_debugMode) return OperationStatus.OK;

        //if (!_feedback.IsEnabled())
        //{
        //    _dashboard.EnableRobot();
        //}

        if ((States)_robotMode != States.Enabled && (States)_robotMode != States.Running) return OperationStatus.RobotMovementFailed;

        _dobotMove.MovL(robotPosition);
        var chrono = Stopwatch.StartNew();

        while (!RobotPosition.AreNear(CurrentPosition, robotPosition, _movementTolerance) && chrono.ElapsedMilliseconds < TimeoutMovement)
        {
            Thread.Sleep(50);
        }

        chrono.Stop();
        return chrono.ElapsedMilliseconds < TimeoutMovement ? OperationStatus.OK : OperationStatus.RobotMovementFailed;
    }

    public OperationStatus MoveLinearToPosition(RobotPosition robotPosition, int speed)
    {
        if (_debugMode) return OperationStatus.OK;

        //if (!_feedback.IsEnabled())
        //{
        //    _dashboard.EnableRobot();
        //}

        if ((States)_robotMode != States.Enabled && (States)_robotMode != States.Running) return OperationStatus.RobotMovementFailed;

        _dobotMove.MovL(robotPosition, speed);
        var chrono = Stopwatch.StartNew();

        while (!RobotPosition.AreNear(CurrentPosition, robotPosition, _movementTolerance) && chrono.ElapsedMilliseconds < TimeoutMovement && !IsInError)
        {
            Thread.Sleep(50);
        }

        chrono.Stop();
        return (chrono.ElapsedMilliseconds < TimeoutMovement && !IsInError) ? OperationStatus.OK : OperationStatus.RobotMovementFailed;
    }

    public OperationStatus MoveLinearToPositionWForceControl(RobotPosition robotPosition, int speed)
    {
        if (_debugMode) return OperationStatus.OK;

        _dobotMove.MovL(robotPosition, speed);

        return OperationStatus.OK;
    }

    public OperationStatus MoveToMultiplePositions(RobotPosition[] robotPositions)
    {
        if (_debugMode) return OperationStatus.OK;

        //if (!_feedback.IsEnabled())
        //{
        //    _dashboard.EnableRobot();
        //}

        if ((States)_robotMode != States.Enabled && (States)_robotMode != States.Running) return OperationStatus.RobotMovementFailed;

        foreach (var robotPosition in robotPositions)
        {
            _dobotMove.MovL(robotPosition);
        }

        var chrono = Stopwatch.StartNew();

        while (!RobotPosition.AreNear(CurrentPosition, robotPositions[^1], _movementTolerance) && chrono.ElapsedMilliseconds < TimeoutMovement)
        {
            Thread.Sleep(20);
        }

        return chrono.ElapsedMilliseconds > TimeoutMovement ? OperationStatus.OK : OperationStatus.Error;
    }

    public void ResetAlarms()
    {
        if (_debugMode) return;

        _dashboard.ResetRobot();
    }

    public void ClearAlarms()
    {
        if (_debugMode) return;

        _dashboard.ClearError();
    }

    public void Connect(string ipAddress, int port, bool debugMode)
    {
        _ipAddress = ipAddress;
        _port = port;
        _debugMode = debugMode;

        if (_debugMode) return;
        if (!_dashboard.Connect(_ipAddress, DashboardPort) ||
            !_dobotMove.Connect(_ipAddress, MovePort) ||
            !_feedback.Connect(_ipAddress, FeedbackPort))
            _isConnected = false;

        _isConnected = true;
        ResetAlarms();
        ClearAlarms();
        Enable();
    }

    public void Disable()
    {
        if (_debugMode) return;

        if (_feedback.IsDisabled()) return;

        _dashboard.DisableRobot();

        var stopwatch = Stopwatch.StartNew();

        while (!_feedback.IsDisabled() && stopwatch.ElapsedMilliseconds < 5 * 1000)
        {
            Thread.Sleep(20);
        }
        stopwatch.Stop();
    }

    public void Disconnect()
    {
        if (_debugMode) return;

        _feedback.Disconnect();
        _dobotMove.Disconnect();
        _dashboard.Disconnect();
        _isConnected = false;
    }

    public void Enable()
    {
        if (_debugMode) return;

        if (_feedback.IsEnabled()) return;

        _dashboard.EnableRobot();

        var stopwatch = Stopwatch.StartNew();

        while (!_feedback.IsEnabled() && stopwatch.ElapsedMilliseconds < 5 * 1000)
        {
            Thread.Sleep(20);
        }
        stopwatch.Stop();
    }

    public string GetErrorID()
    {
        if (_debugMode) return "";
        return _dashboard.GetErrorID();
    }

    public double[] GetAngle()
    {
        if (_debugMode) return new double[0];

        try
        {
			// Ottiene la stringa dal metodo _dashboard.GetAngle()
			string angleData = _dashboard.GetAngle();

			// Verifica se la stringa è nel formato previsto
			if (!string.IsNullOrEmpty(angleData) && angleData.Contains("{") && angleData.Contains("}"))
			{
				// Estrae la parte di stringa tra { e }
				int start = angleData.IndexOf('{') + 1;
				int end = angleData.IndexOf('}');
				string valuesString = angleData.Substring(start, end - start);

				// Divide i valori separati da virgola e li converte in double[]
				string[] values = valuesString.Split(',');
				double[] angles = Array.ConvertAll(values, value => double.Parse(value, CultureInfo.InvariantCulture));

				return angles;
			}
			else
			{
				return [0,0,0,0,0,0];
				// Gestisce il caso in cui la stringa non sia nel formato atteso
				//////throw new FormatException("La stringa restituita non è nel formato previsto.");
			}
		}
        catch (Exception ex)
        {
			return [0, 0, 0, 0, 0, 0];
		}
    }

    public long GetRobotMode()
    {
        if (_debugMode) return -1;
        return _robotMode;
    }
    
    public double[] GetRobotTCPForce()
    {
        //if (_debugMode) return -1;
        return _feedback.FeedbackData.TCPForce;
    }

    public int GetSpeedRatio()
    {
        if (_debugMode) return -1;
        return Convert.ToInt16(_feedback.FeedbackData.SpeedScaling);
    }

    public void MoveJog(JogMovement jogMovement, bool forward)
    {
        if (_debugMode) return;

        _dobotMove.MoveJog(jogMovement switch
        {
            JogMovement.X => $"X{(forward ? "+" : "-")}",
            JogMovement.Y => $"Y{(forward ? "+" : "-")}",
            JogMovement.Z => $"Z{(forward ? "+" : "-")}",
            JogMovement.Yaw => $"Rx{(forward ? "+" : "-")}",
            JogMovement.Pitch => $"Ry{(forward ? "+" : "-")}",
            JogMovement.Roll => $"Rz{(forward ? "+" : "-")}",
            JogMovement.J1 => $"J1{(forward ? "+" : "-")}",
            JogMovement.J2 => $"J2{(forward ? "+" : "-")}",
            JogMovement.J3 => $"J3{(forward ? "+" : "-")}",
            JogMovement.J4 => $"J4{(forward ? "+" : "-")}",
            JogMovement.J5 => $"J5{(forward ? "+" : "-")}",
            JogMovement.J6 => $"J6{(forward ? "+" : "-")}",
            _ => string.Empty
        });
    }

    public void PowerOff()
    {
        if (_debugMode) return;
        _dashboard.PowerOff();
    }

    public void PowerOn()
    {
        if (_debugMode) return;
        _dashboard.PowerOn();
    }

    public bool ReadDigitalInput(int index)
    {
        if (_debugMode) return true;
        try
        {
            long digitalInputs = _feedback.FeedbackData.DigitalInputs;
            string digitalInputsAsString = Convert.ToString(digitalInputs, 2).PadLeft(8, '0');
            return digitalInputsAsString[^index] == '1';
        }
        catch (Exception ex)
        {
            Console.WriteLine($"errore ReadDigitalInput: {ex.Message}");
            return true;
        }
    }

    public bool ReadDigitalOutput(int index)
    {
        if (_debugMode) return true;
        try
        {
            long digitalOutputs = _feedback.FeedbackData.DigitalOutputs;
            string digitalOutputsAsString = Convert.ToString(digitalOutputs, 2).PadLeft(8, '0');
            return digitalOutputsAsString[^index] == '1';
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore ReadDigitalOutput: {ex.Message}");
            return true;
        }
    }

    private void ResetErrors()
    {
        //DEBUG LOG
        System.Diagnostics.Debug.WriteLine("Dobot=ResetErrors()");

        string errorId = GetErrorID();

        if (!errorId.Contains(NoErrorCode))
        {
            ResetAlarms();
            ClearAlarms();
        }
    }

    private void ActivateRobot()
    {
        //DEBUG LOG
        System.Diagnostics.Debug.WriteLine("Dobot=ActivateRobot()");

        string errorId = GetErrorID();

        //if (!errorId.Contains("GetErrorID"))
        //{
        //    return;
        //}

        if (!errorId.Contains(NoErrorCode))
        {
            //DEBUG LOG
            System.Diagnostics.Debug.WriteLine("Dobot=Errori ancora presenti");

            // FUNGO di EMERGENZA
            if (errorId.Contains(emergencyStopPressedCode))
            {
                _errorCode = "emergencyStop";
                //DEBUG LOG
                System.Diagnostics.Debug.WriteLine("Dobot=Fungo Emergenza Attivo");
                return;
            }
            ////////////////////FullReset();
        }


        var currentMode = GetRobotMode();

        //DEBUG LOG
        System.Diagnostics.Debug.WriteLine("Dobot=Mode= " + currentMode);

        if (currentMode == (int)States.Error)
        {
            //DEBUG LOG
            System.Diagnostics.Debug.WriteLine("Dobot=Mode_Error");
            FullReset();
            return;
        }

        if (currentMode == (int)States.PowerOff)
        {
            //DEBUG LOG
            System.Diagnostics.Debug.WriteLine("Dobot=Mode_PowerOff");
            PowerOn();
        }

        while (currentMode == (int)States.PowerOff)
        {
            currentMode = GetRobotMode();
            Thread.Sleep(500);

            // Potrei ottenere un nuovo errore dopo il PowerOn
            errorId = GetErrorID();
            if (!errorId.Contains(NoErrorCode))
            {
                //DEBUG LOG
                System.Diagnostics.Debug.WriteLine("Dobot=Nuovo errore durante il PowerOn");

                FullReset();
                return;
            }
        }

        if (currentMode == (int)States.Disabled)
        {
            //DEBUG LOG
            System.Diagnostics.Debug.WriteLine("Dobot=Mode_Disable");
            Enable();
        }

        if (currentMode == (int)States.DragMode)
        {
            // DRAG MODE Status
            StopDrag();
        }

        /////////////////////////////////////_dashboard.ResetRobot();
    }

    public void FullReset()
    {
        if (_debugMode) return;

        ResetErrors();
        ActivateRobot();       
    }

    public void SetDigitalOutput(int index, bool value)
    {
        if (_debugMode) return;

        if(!_IsInError)
        {
            _dashboard.DigitalOutputs(index, value);
        }
    }

    public async Task SetDigitalOutput(int index, bool value, bool swap)
    {
        if (_debugMode) return;

        //await _dashboard.DigitalOutputs(index, value, swap);

        //////////////////if (!swap)
        //////////////////{
        //////////////////    if (!_IsInError)
        //////////////////    {
        //////////////////        _dashboard.DigitalOutputs(index, value);
        //////////////////    }
        //////////////////}
        //////////////////else
        //////////////////{
        //////////////////    // Se swap è vero e lo status è true, alterna lo stato ogni secondo.
        //////////////////    bool currentStatus = value;
        //////////////////    while (value)  // Continua solo se status è true
        //////////////////    {
        //////////////////        // Alterna lo stato logico
        //////////////////        currentStatus = !currentStatus;

        //////////////////        if (!_IsInError)
        //////////////////        {
        //////////////////            _dashboard.DigitalOutputs(index, currentStatus);
        //////////////////        }

        //////////////////        // Attende 1 secondo prima di alternare nuovamente
        //////////////////        await Task.Delay(5000);
        //////////////////    }

        //////////////////    // Se status diventa false, resettare l'uscita
        //////////////////    if (!_IsInError)
        //////////////////    {
        //////////////////        _dashboard.DigitalOutputs(index, value);
        //////////////////    }
        //////////////////}
    }

    public void SetSpeedRatio(int value)
    {
        if (_debugMode) return;

        int currentSpeed = Convert.ToInt16(_feedback.FeedbackData.SpeedScaling);
        int currentVelocity = Convert.ToInt16(_feedback.FeedbackData.VelocityRatio);
        int currentAcceleration = Convert.ToInt16(_feedback.FeedbackData.AccelerationRatio);
        if (currentSpeed == value) return;

        string resp = _dashboard.SpeedFactor(value);
        var stopwatch = Stopwatch.StartNew();

        while (value != currentSpeed && stopwatch.ElapsedMilliseconds < 5 * 1000)
        {
            Thread.Sleep(20);
            currentSpeed = Convert.ToInt16(_feedback.FeedbackData.SpeedScaling);
        }

        stopwatch.Stop();
    }

    public void StartDrag()
    {
        if (_debugMode) return;

        if (_feedback.FeedbackData.DragStatus == 1) return;

        _dashboard.StartDrag();
        var stopwatch = Stopwatch.StartNew();

        while (_feedback.FeedbackData.DragStatus != 1 && stopwatch.ElapsedMilliseconds < 5 * 1000)
        {
            Thread.Sleep(20);
        }

        stopwatch.Stop();
    }

    public void StopDrag()
    {
        if (_debugMode) return;
        if (_feedback.FeedbackData.DragStatus == 0) return;

        _dashboard.StopDrag();

        var stopwatch = Stopwatch.StartNew();

        while (_feedback.FeedbackData.DragStatus != 0 && stopwatch.ElapsedMilliseconds < 5 * 1000)
        {
            Thread.Sleep(20);
        }

        stopwatch.Stop();
    }

    public void StopJog()
    {
        if (_debugMode) return;
        _dobotMove.StopMoveJog();
    }
    
    public void SetToolFrame(int Tool, int X, int Y, int Z, int R)
    {
        if (_debugMode) return;
        _dashboard.SetToolFrame(Tool, X, Y, Z, R);
    }

    public void SetUserFrame(int Frame, int X, int Y, int Z, int R)
    {
        if (_debugMode) return;
        _dashboard.SetUserFrame(Frame, X, Y, Z, R);
    }


    //string errorId = GetErrorID();

    //if (!errorId.Contains(NoErrorCode))
    //{
    //    //DEBUG LOG
    //    System.Diagnostics.Debug.WriteLine("Dobot=Errori ancora presenti");


    //    if (errorId.Contains(emergencyStopTriggeredCode))
    //    {
    //        // FUNGO di EMERGENZA intervenuto
    //        _errorCode = "emergencyStopTrg";
    //        //return;
    //    }

    //    if (errorId.Contains(elbowSingularityCode))
    //    {
    //        // errore di Singolarità
    //        _errorCode = "elbowSingularity";
    //        //return;
    //    }

    //    if (errorId.Contains(abnormalEmergencyStopCode))
    //    {
    //        // errore di abnormal Emergency
    //        _errorCode = "abEmergencyStop";
    //    }
    //    if (errorId.Contains(emergencyStopPressedCode))
    //    {
    //        _errorCode = "emergencyStop";
    //        //DEBUG LOG
    //    }

    //}


}
