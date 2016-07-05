using UnityEngine;
using System.Collections;

public static class BaseGameEntityID
{
    static int m_ID = 0;

    public static int GUID
    {
        get
        {
            int tmp = m_ID;

            m_ID++;

            return tmp;
        }
    }
}

abstract public class BaseGameEntity
{
    //  实体唯一标识符(Unity里基本用GameObject的InstacneID来表示)
    int m_ID;

    //  
    void SetID(int val)
    {
        m_ID = val;
    }

    //////////////////////////////////////
    public BaseGameEntity(int id)
    {
        SetID(BaseGameEntityID.GUID);
    }

    //  所有的派生都必须实现
    public abstract void Update();

    public int ID
    {
        get { return m_ID; }
    }

    //  所有的实体entities都通过此方法实现消息传送
    //  使用MessageDispatcher类
    public abstract bool HandleMessage(Telegram msg);// { Debug.LogException(new System.Exception("No Overwrite Method")); return false; }

}