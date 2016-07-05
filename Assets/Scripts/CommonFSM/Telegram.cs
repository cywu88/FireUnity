using UnityEngine;
using System.Collections;

public enum MouseInfo
{
    None,
    OneClick,
    TwoClick,
    LongPress,
    PressedMoved,
}

/// <summary>
/// 消息类型，根据Entity的类型来增加
/// </summary>
public enum MsgType
{
    MSG_NULL,
    MSG_MOUSE,      //  输入设备：鼠标
    MSG_PLAYER,     //  主角
    MSG_ENEMY,     //  主角
	MSG_SKILL,
}

/// <summary>
/// 消息体
/// </summary>
public class TelegramInfo
{
    //public TIMouse mouse = new TIMouse();
    //public TIPlayer player = new TIPlayer();
    //public TIEnemy enemy = new TIEnemy();
}

public class Telegram 
{
    //the entity that sent this telegram
    public int Sender;

    //the entity that is to receive this telegram
    public int Receiver;

    //the message itself. These are all enumerated in the file
    //"MessageTypes.h"
    public int Msg;

    //messages can be dispatched immediately or delayed for a specified amount
    //of time. If a delay is necessary this field is stamped with the time 
    //the message should be dispatched.
    public double DispatchTime;

    //any additional information that may accompany the message
    public TelegramInfo ExtraInfo;

    public Telegram()
    {
        DispatchTime    = -1;
        Sender          = -1;
        Receiver        = -1;
        Msg             = 0;
    }

    public Telegram(double time,
             int sender,
             int receiver,
             int msg,
             TelegramInfo info = null)
    {
        DispatchTime = time;
        Sender = sender;
        Receiver = receiver;
        Msg = msg;
        ExtraInfo = info;
    }
}
