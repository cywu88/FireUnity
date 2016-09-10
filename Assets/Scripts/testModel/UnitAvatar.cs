using UnityEngine;
using System.Collections;
using TianyuCommon.Plugins;
using System.IO;
using System.Collections.Generic;
using ResourceManager;
using CommonLang;
using System;
using CommonAI.Zone;
using Assets.Scripts.Setting;

/*@                                   /\  /\
* @                                  /  \/  \                        
*  @                                /        --                      
*   \---\                          /           \                     
*    |   \------------------------/       /-\    \                   
*    |                                    \-/     \                  
*     \                                             ------O
*      \            玩家时装                              /           
*       |    |                    |    |                /            
*       |    |                    |    |-----    -------             
*       |    |\                  /|    |     \  WWWWWW/              
*       |    | \                / |    |      \-------              
*       |    |  \--------------/  |    |
*      /     |                   /     |
*      \      \                  \      \
*       \-----/                   \-----/
*/

public class UnitAvatar
{
    #region 属性

    //时装信息
    private AvatarResInfo mInfo;

    //加载回调
    private LoadAvatarCallBack mBack;
    public delegate void LoadAvatarCallBack(UnityEngine.GameObject o, object userdata);

    //模型
    private int mBodyId = 0;
    private GameObject mBody = null;

    

    //武器
    private HashMap<int, GameObject> mObjs;

    //单位uuid
    private string mUuid;


    //是否是玩家自己
    private bool mIsActor = false;

    #endregion

    #region 周期

    public UnitAvatar()
    {

    }

    public void Init(string id, bool actor = false)
    {
        mIsActor = actor;
        if (string.IsNullOrEmpty(mUuid))
        {
            mUuid = id;
            mObjs = new HashMap<int, GameObject>();
        }
    }

    public void UnLoad()
    {
        Clear();

        mObjs = null;
        mBack = null;
    }

    public void UnLoadPart()
    {
        ClearObj();
        mObjs = null;
        mBack = null;
    }

    #endregion

    #region 外部调用

    public void LoadAvatar(UnitInfo info, List<TianyuAvatarInfo> avatar, LoadAvatarCallBack back)
    {
        ClearObj();

        mInfo = new AvatarResInfo(info, avatar,mIsActor);
        mBack = back;

        LoadBody();
    }

    #endregion

    #region 内部加载

    private void LoadBody()
    {
        mBodyId = GetResId();
        string pathname = Path.GetFileNameWithoutExtension(mInfo.BodyFile);
         UnitLoader uLoader = new UnitLoader(mInfo.BodyFile, pathname,
             ( o, data,  isLoadOK) =>
             {
                 if (o != null && mBodyId == (int)data)
                 {
                     mBody = o;
                     UIDepthUnit.SetLayer(mBody, 0);
                     LoadPart();
                 }
             }, mBodyId
        );
    }

    private void LoadPart()
    {
        int partcount = mInfo.Parts.Count;

        if (partcount == 0)
        {
            if (mBack != null)
            {
                mBack(mBody, mUuid);
            }
        }

        foreach (AvatarResInfo.AvatarPart item in mInfo.Parts)
        {
            int resId = GetResId();

            AvatarPartRes r = new AvatarPartRes();
            r.BodyId = mBodyId;
            r.ResId = resId;
            r.PartTag = item.PartTag;

            AddObj(resId);

            string pathname = Path.GetFileNameWithoutExtension(item.FileName);
            UnitLoader uLoader = new UnitLoader(item.FileName, pathname,
                (obj, data, isLoadOK) =>
                {
                    if (obj != null)
                    {
                        if (data is AvatarPartRes && (data as AvatarPartRes).BodyId == mBodyId)
                        {
                            UIDepthUnit.SetLayer(obj, 0);
                            AddObj((data as AvatarPartRes).ResId, obj);

                            GameObject p = getObjonPart((data as AvatarPartRes).PartTag, mBody);
                            if (p != null)
                            {
                                obj.transform.SetParent(p.transform);
                                obj.transform.localPosition = Vector3.zero;
                                obj.transform.localScale = Vector3.one;
                                obj.transform.localRotation = Quaternion.identity;
                            }

                            if (--partcount == 0)
                            {
                                if (mBack != null)
                                {
                                    mBack(mBody, mUuid);
                                    mBack = null;
                                }
                            }
                        }
                    }
                }, r
            );
        }
    }

    private int GetResId()
    {
        return Guid.NewGuid().GetHashCode();
    }

    //请求一个就加到列表里,方便判断是否加载完毕,用于取消请求
    private void AddObj(int id)
    {
        if (mObjs != null)
        {
            mObjs.Add(id, null);
        }
        else
        {
            Debuger.LogWarning("时装回调空了  "+ mUuid);
        }
    }

    //加载完成后赋给对应资源id,方便清理
    private void AddObj(int id, GameObject go)
    {
        if (mObjs != null)
        {
            if (mObjs.ContainsKey(id))
            {
                mObjs[id] = go;
            }
        }
        else
        {
            Debuger.LogError("LogWarning  " + mUuid);
        }
    }

    #endregion

    #region 清理

    public void Clear()
    {
        ClearObj();
        ClearBody();
    }

    private void ClearBody()
    {
        if (mBody != null)
        {
            ResourceMgr.Instance.CollectUnitInstance(mBody);
            mBody = null;
        }
        else
        {
            ResourceMgr.Instance.CancelRequestUnitInstance(mBodyId);
        }
    }

    private void ClearObj()
    {
        foreach (int item in mObjs.Keys)
        {
            if (mObjs[item] == null)
            {
                ResourceMgr.Instance.CancelRequestUnitInstance(item);
            }
            else
            {
                ResourceMgr.Instance.CollectUnitInstance(mObjs[item]);
            }
        }

        mObjs.Clear();
    }

    #endregion

    #region 通用方法

    public static UnityEngine.GameObject getObjonPart(string partname, UnityEngine.GameObject gameobj)
    {
        UnityEngine.GameObject obj = null;

        if (gameobj != null)
        {
            Transform[] tfs = gameobj.GetComponentsInChildren<Transform>();
            string temp = null;
            foreach (Transform t in tfs)
            {
                temp = t.name;
                if (temp == partname)
                {
                    obj = t.gameObject;
                    break;
                }
            }
        }
        return obj;
    }

    #endregion

}

public class AvatarResInfo
{
    public class AvatarPart
    {
        public string PartTag = "";
        public string FileName = "";
    }

    public string BodyFile = "";
    
    public List<AvatarPart> Parts = new List<AvatarPart>();
    
    //把服务器不处理的字符串转换成用于加载的数据
    //同时还要处理隐藏时装的情况
    public AvatarResInfo(UnitInfo info, List<TianyuAvatarInfo> avatar, bool actor)
    {
        foreach (TianyuAvatarInfo item in avatar)
        {
            if (item.PartTag == TianyuAvatarInfo.TianyuAvatar.Avatar_Body)
            {
                if (ProjectSetting.GetGameSetting((int)ProjectSetting.SettingType.GameSetting_Di) == 1)
                {

                }
                else if (actor || ProjectSetting.GetGameSetting((int)ProjectSetting.SettingType.GameSetting_Fashion) == 1)
                {
                    BodyFile = item.FileName;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(item.FileName))
                {
                    string[] sss = item.FileName.Split(';');
                    foreach (string ss in sss)
                    {
                        string[] s = ss.Split(',');
                        if (s != null && s.Length == 2)
                        {
                            AvatarPart p = new AvatarPart();
                            p.PartTag = s[0];
                            p.FileName = s[1];

                            Parts.Add(p);
                        }
                    }
                }
            }
        }
        if (string.IsNullOrEmpty(BodyFile))
        {
            BodyFile = info.FileName;
        }
    }
}

public class AvatarPartRes
{
    public int BodyId;
    public int ResId;
    public string PartTag;
}
