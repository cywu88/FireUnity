using UnityEngine;
using System.Collections;

/// <summary>
/// 消息发送机制，主要用于各个实体间的消息传递
/// </summary>
public class MessageDispatcher 
{
    private static MessageDispatcher self;

    public static MessageDispatcher Instance()
    {
        if (self == null)
            self = new MessageDispatcher();

        return self;
    }

    /// <summary>
    /// 消息传递
    /// </summary>
    /// <param name="pReceiver"></param>
    /// <param name="telegram"></param>
    void Discharge(BaseGameEntity pReceiver, Telegram telegram)
    {
        if (!pReceiver.HandleMessage(telegram))
        {
            //telegram could not be handled
            //cout << "Message not handled";
        }
    }

    /// <summary>
    /// 全局方法，提供消息发送的功能
    /// </summary>
    /// <param name="delay">延迟时间(目前不用)</param>
    /// <param name="sender"></param>
    /// <param name="receiver"></param>
    /// <param name="msg"></param>
    /// <param name="ExtraInfo"></param>
    public void DispatchMessage(double  delay,
                                 int    sender,
                                 int    receiver,
                                 int    msg,
                                 TelegramInfo ExtraInfo)
    {

        //  发送消息的Entity
        BaseGameEntity pSender   = EntityManager.Instance().GetEntityFromID(sender);
        //  接收消息的Entity
        BaseGameEntity pReceiver = EntityManager.Instance().GetEntityFromID(receiver);

        //  
        if (pReceiver == null)
            return;
  
        //  消息体
        Telegram telegram = new Telegram(0, sender, receiver, msg, ExtraInfo);
  
        //  发送
        Discharge(pReceiver, telegram);
    }
}
