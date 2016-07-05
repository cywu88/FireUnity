using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityManager
{
    private static EntityManager self;

    public static EntityManager Instance()
    {
        if (self == null)
            self = new EntityManager();

        return self;
    }

    //  管理器
    Dictionary<int, BaseGameEntity> m_EntityMap = new Dictionary<int, BaseGameEntity>();

    //  注册实体对象
    public void RegisterEntity(BaseGameEntity NewEntity)
    {
        m_EntityMap.Add(NewEntity.ID, NewEntity);
    }

    //  根据实体对象的ID返回对象
    public BaseGameEntity GetEntityFromID(int id)
    {
        return m_EntityMap[id];
    }

    //  移除实体对象
    public void RemoveEntity(BaseGameEntity pEntity)
    {
        m_EntityMap.Remove(pEntity.ID);
    }
}
