using UnityEngine;
using System.Collections;
using TianyuCommon.Plugins;
using System.Collections.Generic;
using CommonAI.Zone;
using TianyuCommon.Message;
using Assets.Scripts.Setting;

partial class ComAIUnit
{
    public bool IsLoadAvatar { get; private set; }

    private bool mIsMorph = false;
    private PlayAvatarEventB2C mChangeAvatar = null;

    private UnitAvatar mAvatarLoader = new UnitAvatar();

    public void InitLoadAvatar()
    {
        if (!ProjectSetting.IsBattleRunStart && (this is ComAIActor || this is ComAIPlayer))
        {
            IsLoadAvatar = true;
        }
        else
        {
            IsLoadAvatar = false;
        }
    }

    //加载时装
    public void LoadAvatar()
    {
        this.RUnit.SetAvatar(mAvatarLoader);
        mAvatarLoader.Init(ZUnit.PlayerUUID, this is ComAIActor);

        PlayerVisibleDataB2C visibleinfo = this.ZUnit.SyncInfo.VisibleInfo as PlayerVisibleDataB2C;
        if (visibleinfo != null)
        {
            (this.ZUnit.Info.Properties as TianyuUnitProperties).ServerData.AvatarList = visibleinfo.AvatarList;
        }

        TianyuUnitData serverdata = ((TianyuUnitProperties)ZUnit.Info.Properties).ServerData;
        mAvatarLoader.LoadAvatar(ZUnit.Info, serverdata.AvatarList, LoadAvatarBack);
    }

    //加载回调
    public void LoadAvatarBack(UnityEngine.GameObject o, object userdata)
    {
        SetLayer(o, 8);

        o.name = this.Name;
        this.RUnit.PushCharactor(o);

        OnLoadOK(o);

        SimpleDelegate act = this.EventLisers.Get(EVENT_LOADOK);
        if (act != null)
        {
            act.AssginData(this);
            act.InvokeAction();
        }
    }

    //时装设置
    public void OnPlayAvatarSet()
    {
        mAvatarLoader.LoadAvatar(ZUnit.Info, (this.ZUnit.Info.Properties as TianyuUnitProperties).ServerData.AvatarList, UpdateAvatarBack);
    }

    //时装变化
    public void OnPlayAvatarChange(PlayAvatarEventB2C evt)
    {
        if (mIsMorph)
        {
            mChangeAvatar = evt;
        }
        else
        {
            mChangeAvatar = null;
            (ZUnit.Info.Properties as TianyuUnitProperties).ServerData.AvatarList = evt.AvatarList;
            if (this is ComAIActor)
            {
                BattleClientBase.SaveActorProp();
            }
            mAvatarLoader.LoadAvatar(ZUnit.Info, evt.AvatarList, UpdateAvatarBack);
        }
    }

    //变化回调
    public void UpdateAvatarBack(UnityEngine.GameObject o, object userdata)
    {
        if (o != null)
        {
            this.RUnit.PopAvatar();

            SetLayer(o, 8);
            o.name = this.Name;
            this.RUnit.PushCharactor(o);

            ShadowEffect = o.GetComponent<UnitShadow>();
            if (ShadowEffect == null)
            {
                ShadowEffect = o.AddComponent<UnitShadow>();
            }
            ShadowEffect.Init(o, ZUnit.Info.BodySize, UnitType.Unit_Actor, ZUnit.Force);

            if (this.ZUnit.CurrentState == CommonAI.Zone.Helper.UnitActionStatus.Move)
            {
                this.ChangeState(new GBattleState_MOVE());
            }
            else if (this.ZUnit.CurrentState == CommonAI.Zone.Helper.UnitActionStatus.Idle)
            {
                this.ChangeState(new GBattleState_IDLE());
            }

            RemoveBar();
            AddInfoBar(o);
        }
        else
        {
            Debuger.LogError("时装资源没有"+ userdata);
        }
    }


    //设置物体层级
    public static void SetLayer(GameObject o, int l)
    {
        if (o != null)
        {
            Transform[] transformList = o.GetComponentsInChildren<Transform>();
            foreach (Transform item in transformList)
            {
                item.gameObject.layer = l;
            }
        }
    }
}
