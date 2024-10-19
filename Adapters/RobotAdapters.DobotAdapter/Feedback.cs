using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobotAdapters.DobotAdapter;

class Feedback : DobotClient
{
    private Thread mThread;

    public FeedbackData FeedbackData { get; private set; }

    public bool DataHasRead { get; set; }

    public Feedback()
    {
        FeedbackData = new FeedbackData();
    }

    protected override void OnConnected(Socket sock)
    {
        sock.SendTimeout = 5000;
        //sock.ReceiveTimeout = 15000;

        mThread = new Thread(OnRecvData);
        mThread.IsBackground = true;
        mThread.Start();
    }

    protected override void OnDisconnected()
    {
        if (null != mThread && mThread.IsAlive)
        {
            try
            {
                mThread.Abort();
                mThread = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("close thread:" + ex.ToString());
            }
        }
    }

    /// <summary>
    /// 接收返回的数据并解析处理
    /// </summary>
    private void OnRecvData()
    {
        byte[] buffer = new byte[4320];//1440*3
        int iHasRead = 0;
        while (IsConnected())
        {
            try
            {
                int iRet = Receive(buffer, iHasRead, buffer.Length - iHasRead, SocketFlags.None);
                if (iRet <= 0)
                {
                    continue;
                }
                iHasRead += iRet;
                if (iHasRead < 1440)
                {
                    continue;
                }

                bool bHasFound = false;//是否找到数据包头了
                for (int i=0; i<iHasRead; ++i)
                {
                    //找到消息头
                    int iMsgSize = buffer[i+1];
                    iMsgSize <<= 8;
                    iMsgSize |= buffer[i];
                    iMsgSize &= 0x00FFFF;
                    if (1440 != iMsgSize)
                    {
                        continue;
                    }
                    //校验
                    ulong checkValue = BitConverter.ToUInt64(buffer, i + 48);
                    if (0x0123456789ABCDEF == checkValue)
                    {//找到了校验值
                        bHasFound = true;
                        if (i != 0)
                        {//说明存在粘包，要把前面的数据清理掉
                            iHasRead = iHasRead - i;
                            Array.Copy(buffer, i, buffer, 0, buffer.Length-i);
                        }
                        break;
                    }
                }
                if (!bHasFound)
                {//如果没找到头，判断数据长度是不是快超过了总长度，超过了，说明数据全都有问题，删掉
                    if (iHasRead >= buffer.Length) iHasRead = 0;
                    continue;
                }
                //再次判断字节数是否够
                if (iHasRead < 1440)
                {
                    continue;
                }
                iHasRead = iHasRead - 1440;
                //按照协议的格式解析数据
                ParseData(buffer);
                Array.Copy(buffer, 1440, buffer, 0, buffer.Length - 1440);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("recv thread:" + ex.ToString());
            }
        }
    }

    /// <summary>
    /// 解析数据
    /// </summary>
    /// <param name="buffer">一包完整的数据</param>
    private void ParseData(byte[] buffer)
    {
        int iStartIndex = 0;

        FeedbackData.MessageSize = BitConverter.ToInt16(buffer, iStartIndex); //unsigned short
        iStartIndex += 2;

        for (int i = 0; i < FeedbackData.Reserved1.Length; ++i)
        {
            FeedbackData.Reserved1[i] = BitConverter.ToInt16(buffer, iStartIndex);
            iStartIndex += 2;
        }

        FeedbackData.DigitalInputs = BitConverter.ToInt64(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.DigitalOutputs = BitConverter.ToInt64(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.RobotMode = BitConverter.ToInt64(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.TimeStamp = BitConverter.ToInt64(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.Reserved2 = BitConverter.ToInt64(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.TestValue = BitConverter.ToInt64(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.Reserved3 = BitConverter.ToInt64(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.SpeedScaling = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.LinearMomentumNorm = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.VMain = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.VRobot = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.IRobot = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.Reserved4 = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.Reserved5 = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        for (int i = 0; i < FeedbackData.ToolAccelerometerValues.Length; ++i)
        {
            FeedbackData.ToolAccelerometerValues[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.ElbowPosition.Length; ++i)
        {
            FeedbackData.ElbowPosition[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.ElbowVelocity.Length; ++i)
        {
            FeedbackData.ElbowVelocity[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.QTarget.Length; ++i)
        {
            FeedbackData.QTarget[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.QdTarget.Length; ++i)
        {
            FeedbackData.QdTarget[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.QddTarget.Length; ++i)
        {
            FeedbackData.QddTarget[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.ITarget.Length; ++i)
        {
            FeedbackData.ITarget[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.MTarget.Length; ++i)
        {
            FeedbackData.MTarget[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.QActual.Length; ++i)
        {
            FeedbackData.QActual[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.QdActual.Length; ++i)
        {
            FeedbackData.QdActual[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.IActual.Length; ++i)
        {
            FeedbackData.IActual[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.IControl.Length; ++i)
        {
            FeedbackData.IControl[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.ToolVectorActual.Length; ++i)
        {
            FeedbackData.ToolVectorActual[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.TCPSpeedActual.Length; ++i)
        {
            FeedbackData.TCPSpeedActual[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.TCPForce.Length; ++i)
        {
            FeedbackData.TCPForce[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.ToolVectorTarget.Length; ++i)
        {
            FeedbackData.ToolVectorTarget[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.TCPSpeedTarget.Length; ++i)
        {
            FeedbackData.TCPSpeedTarget[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.MotorTempetatures.Length; ++i)
        {
            FeedbackData.MotorTempetatures[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.JointModes.Length; ++i)
        {
            FeedbackData.JointModes[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.VActual.Length; ++i)
        {
            FeedbackData.VActual[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.Handtype.Length; ++i)
        {
            FeedbackData.Handtype[i] = buffer[iStartIndex];
            iStartIndex += 1;
        }

        FeedbackData.User = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.Tool = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.RunQueuedCmd = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.PauseCmdFlag = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.VelocityRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.AccelerationRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.JerkRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.XYZVelocityRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.RVelocityRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.XYZAccelerationRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.RAccelerationRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.XYZJerkRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.RJerkRatio = buffer[iStartIndex];
        iStartIndex += 1;

        FeedbackData.BrakeStatus = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.EnableStatus = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.DragStatus = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.RunningStatus = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.ErrorStatus = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.JogStatus = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.RobotType = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.DragButtonSignal = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.EnableButtonSignal = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.RecordButtonSignal = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.ReappearButtonSignal = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.JawButtonSignal = buffer[iStartIndex];
        iStartIndex += 1;
        FeedbackData.SixForceOnline = buffer[iStartIndex];
        iStartIndex += 1;

        for (int i = 0; i < FeedbackData.Reserved6.Length; ++i)
        {
            FeedbackData.Reserved6[i] = buffer[iStartIndex];
            iStartIndex += 1;
        }

        for (int i = 0; i < FeedbackData.MActual.Length; ++i)
        {
            FeedbackData.MActual[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        FeedbackData.Load = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.CenterX = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.CenterY = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        FeedbackData.CenterZ = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        for (int i = 0; i < FeedbackData.UserValu.Length; ++i)
        {
            FeedbackData.UserValu[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.Tools.Length; ++i)
        {
            FeedbackData.Tools[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        FeedbackData.TraceIndex = BitConverter.ToDouble(buffer, iStartIndex);
        iStartIndex += 8;

        for (int i = 0; i < FeedbackData.SixForceValue.Length; ++i)
        {
            FeedbackData.SixForceValue[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.TargetQuaternion.Length; ++i)
        {
            FeedbackData.TargetQuaternion[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.ActualQuaternion.Length; ++i)
        {
            FeedbackData.ActualQuaternion[i] = BitConverter.ToDouble(buffer, iStartIndex);
            iStartIndex += 8;
        }

        for (int i = 0; i < FeedbackData.Reserved7.Length; ++i)
        {
            FeedbackData.Reserved7[i] = buffer[iStartIndex];
            iStartIndex += 1;
        }

        this.DataHasRead = true;
    }

    public string ConvertRobotMode()
    {
        switch (FeedbackData.RobotMode)
        {
            case FeedbackData.NO_CONTROLLER:
                return "NO_CONTROLLER";
            case FeedbackData.NO_CONNECTED:
                return "NO_CONNECTED";
            case FeedbackData.ROBOT_MODE_INIT:
                return "ROBOT_MODE_INIT";
            case FeedbackData.ROBOT_MODE_BRAKE_OPEN:
                return "ROBOT_MODE_BRAKE_OPEN";
            case FeedbackData.ROBOT_POWEROFF:
                return "ROBOT_POWEROFF";
            case FeedbackData.ROBOT_MODE_DISABLED:
                return "ROBOT_MODE_DISABLED";
            case FeedbackData.ROBOT_MODE_ENABLE:
                return "ROBOT_MODE_ENABLE";
            case FeedbackData.ROBOT_MODE_BACKDRIVE:
                return "ROBOT_MODE_BACKDRIVE";
            case FeedbackData.ROBOT_MODE_RUNNING:
                return "ROBOT_MODE_RUNNING";
            case FeedbackData.ROBOT_MODE_RECORDING:
                return "ROBOT_MODE_RECORDING";
            case FeedbackData.ROBOT_MODE_ERROR:
                return "ROBOT_MODE_ERROR";
            case FeedbackData.ROBOT_MODE_PAUSE:
                return "ROBOT_MODE_PAUSE";
            case FeedbackData.ROBOT_MODE_JOG:
                return "ROBOT_MODE_JOG";
        }
        return string.Format("UNKNOW：RobotMode={0}", FeedbackData.RobotMode);
    }

    public bool IsEnabled()
    {
        return FeedbackData.ROBOT_MODE_ENABLE == FeedbackData.RobotMode;
    }
    public bool IsDisabled()
    {
        return FeedbackData.ROBOT_MODE_DISABLED == FeedbackData.RobotMode;
    }
    public bool IsInError()
    {
        return FeedbackData.ROBOT_MODE_ERROR == FeedbackData.RobotMode;
    }
}
