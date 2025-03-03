﻿using Entities;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Utilities.core;

namespace RobotAdapters.DobotAdapter;

class DobotMove : DobotClient
{
    protected override void OnConnected(Socket sock)
    {
        sock.SendTimeout = 5000;
        sock.ReceiveTimeout = 15000;
    }

    protected override void OnDisconnected()
    {
    }

    /// <summary>
    /// 关节点动运动，不固定距离运动
    /// </summary>
    /// <param name="s">点动运动轴</param>
    /// <returns>返回执行结果的描述信息</returns>
    public string MoveJog(string s)
    {
        if (!IsConnected())
        {
            return "device does not connected!!!";
        }

        string str;
        if (string.IsNullOrEmpty(s))
        {
            str = "MoveJog()";
        }
        else
        {
            string strPattern = "^(J[1-6][+-])|([XYZ][+-])|(R[xyz][+-])$";
            if (Regex.IsMatch(s, strPattern))
            {
                str = "MoveJog(" + s + ")";
            }
            else
            {
                return "send error:invalid parameter!!!";
            }
        }
        if (!SendData(str))
        {
            return str + ":send error";
        }

        return WaitReply(5000);
    }
    /// <summary>
    /// 停止关节点动运动
    /// </summary>
    /// <returns>返回执行结果的描述信息</returns>
    public string StopMoveJog()
    {
        return MoveJog(null);
    }

    /// <summary>
    /// 点到点运动，目标点位为笛卡尔点位
    /// </summary>
    /// <param name="pt">笛卡尔点位</param>
    /// <returns>返回执行结果的描述信息</returns>
    public string MovJ(RobotPosition rp)
    {
        if (!IsConnected())
        {
            return "device does not connected!!!";
        }

        if (null == rp)
        {
            return "send error:invalid parameter!!!";
        }
        string str = $"MovJ({rp.X.ToInvariantString()},{rp.Y.ToInvariantString()},{rp.Z.ToInvariantString()},{rp.Yaw.ToInvariantString()},{rp.Pitch.ToInvariantString()},{rp.Roll.ToInvariantString()})";
        if (!SendData(str))
        {
            return str + ":send error";
        }

        return WaitReply(5000);
    }

    /// <summary>
    /// 直线运动，目标点位为笛卡尔点位
    /// </summary>
    /// <param name="pt">笛卡尔点位</param>
    /// <returns>返回执行结果的描述信息</returns>
    public string MovL(RobotPosition rp)
    {
        if (!IsConnected())
        {
            return "device does not connected!!!";
        }
        if (null == rp)
        {
            return "send error:invalid parameter!!!";
        }
        string str = $"MovL({rp.X.ToInvariantString()},{rp.Y.ToInvariantString()},{rp.Z.ToInvariantString()},{rp.Yaw.ToInvariantString()},{rp.Pitch.ToInvariantString()},{rp.Roll.ToInvariantString()})";
        if (!SendData(str))
        {
            return str + ":send error";
        }

        return WaitReply(5000);
    }

    public string MovL(RobotPosition rp, int speed)
    {
        if (!IsConnected())
        {
            return "device does not connected!!!";
        }
        if (null == rp)
        {
            return "send error:invalid parameter!!!";
        }
        string str = $"MovL({rp.X.ToInvariantString()},{rp.Y.ToInvariantString()},{rp.Z.ToInvariantString()},{rp.Yaw.ToInvariantString()},{rp.Pitch.ToInvariantString()},{rp.Roll.ToInvariantString()},SpeedL={speed})";
        if (!SendData(str))
        {
            return str + ":send error";
        }

        return WaitReply(5000);
    }
}
