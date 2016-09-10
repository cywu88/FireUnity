using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CommonUnity3D.MFUnity.LoadUtil;
using CommonLang;
using System.IO;
using TianyuCommon.Plugins;
using Assets.Scripts.Setting;
using ResourceManager;
using System;
using Assets.Scripts.Game.Story;

/// <summary>
/// 渲染节点.
/// </summary>
public class RenderUnit : System.IDisposable
{
    //所有RenderUnit的根节点.
    private static GameObject root;
    public const string PART_REAR_BUFF = "rear_buff";
    public const string PART_HEAD_BUFF = "head_buff";
    public const string PART_CHEST_BUFF = "chest_buff";
    public const string PART_FOOT_BUFF = "foot_buff";
    public const string PART_RIGHT_SPELL = "RightHand_Spell";//法师特效挂载点
    public const string PART_LEFT_SPELL = "LeftHand_Spell";
    public const string PART_RIGHT_HAND = "RightHand_Weapon";//法师特效挂载点
    public const string PART_LEFT_HAND = "LeftHand_Weapon";
    public const string PART_FIRSTGLOVE = "Female_FirstQT";//初始拳套
    public const string PART_HEAD = "Bip001 HeadNub";//头部节点
    public const string PART_COFFIN = "Rear_Weapon";//棺材节点
    //挂载节点维护表.
    private Dictionary<string, GameObject> mPartMap = new Dictionary<string, GameObject>();
    //武器挂载节点维护表.
    private Dictionary<string, GameObject> mWeaponPartMap = new Dictionary<string, GameObject>();
    private static HashMap<string, AnimationClip> animationMap = new HashMap<string, AnimationClip>();
    private AssetBoundleLoader mAssetloader;
    private string[] m_AnimationNameList = null;
    private List<string> mAnimationSpeical = new List<string>();//特殊动作加载
    private List<Renderer> mRenderList = new List<Renderer>();//缓存所有render 做隐身处理
    private bool isFirstLoad = true;
    private FresnelControlManager m_Fcm;//菲尼尔受伤霸体效果
    public bool IsPlayer = false;//是否是玩家
    public string mLockAction = null;//锁定动作
    public bool m_IsShow = true;//玩家显示
    private Dictionary<string, Material> mMaterialMap = new Dictionary<string, Material>();//初始化玩家材质球
    private string[] GoldenWeaponNodeList = new string[] { PART_LEFT_HAND, PART_RIGHT_HAND };//金身武器节点

    public bool IsActor { get; set; }

    public enum AtomType
    {
        Positon,
        Direction,
        Scale
    }
    public delegate void OnEnterStateActionHandle(RenderUnit cell, int state, object p);
    public delegate void OnAtomHandle(AtomType type, GameObject obj);
    private OnAtomHandle _OnAtom;
    public event OnAtomHandle OnAtomEvent { add { _OnAtom += value; } remove { _OnAtom -= value; } }
    private float m_time;//测试残影的time
    /// <summary>
    ///每个UnitInfo对应的一个场景中的单体.
    /// </summary>
    public GameObject RootObject { get; private set; }
    /// <summary>
    /// 挂载粒子效果节点.
    /// </summary>
    public GameObject EffectNode { get; private set; }
    /// <summary>
    /// 被击闪烁效果.
    /// </summary>
    private Blink blinkPart = null;
    /// <summary>
    /// 是否被销毁.
    /// </summary>
    public bool IsDestroy { get; private set; }

    public uint ObjId { get; set; }

    public bool mActive = true;
    private Shader m_shader;

    private AnimationInfo m_currentInfo = null;//当前技能信息
    private bool m_bStopFrame = false;//定帧
    /// <summary>
    /// 控制激活.
    /// </summary>
    public bool Active
    {
        set
        {
            if (mActive != value)
            {
                mActive = value;
                //if (RootObject != null) { RootObject.SetActive(mActive); }
                if (RootObject != null)
                {
                    UIDepthUnit.SetLayer(RootObject, value == true ? 8 : 11);
                }
            }
        }
        get { return mActive; }
    }

    /// <summary>
    /// 动画播放信息.
    /// </summary>
    public class AnimationInfo
    {
        public delegate void AnimationCallBack(AnimationInfo sender);
        public AnimationCallBack OnAnimationCallBack;
        public string Name { get; set; }
        public WrapMode Mode { get; set; }
        public float Speed { get; set; }
        public float Duration { get; set; }
        public float DelayTime { get; set; }
        //InDelayState为true时,才能进行播放.
        public bool InDelayState { get; set; }
        public bool DestroyAnimation { get; set; }
        //隐身
        public bool m_bIsInvisible { get; set; }
        //设置此项则在清楚动画时必须回调
        public bool mMustCallBack = false;

        public bool IsCallBack = false;
        public AnimationInfo(string name, float duration, WrapMode mode, float speed, float delay, bool isInvisible = false, bool mustCallBack = false)
        {
            this.Name = name;
            this.Mode = mode;
            this.Speed = speed;
            this.Duration = duration;
            this.DelayTime = delay;
            //this.InDelayState = true;
            this.DestroyAnimation = false;
            this.m_bIsInvisible = isInvisible;
            this.mMustCallBack = mustCallBack;
        }

        public void CallBack()
        {
            if (OnAnimationCallBack != null && !IsCallBack) { IsCallBack = true; OnAnimationCallBack.Invoke(this); }
            OnAnimationCallBack = null;
        }
    }

    #region Avatar相关
    public AvatarSuit Avatar;

    public void ResetAvatar(Dictionary<string, KeyValuePair<string, string>> avatar)
    {
        string filename = avatar["Avatar_Body"].Key;
        string name = avatar["Avatar_Body"].Value;
        //Avatar = new AvatarSuit(filename, name);
        foreach (var part in avatar)
        {
            if (part.Key != "Avatar_Body")
            {
                Avatar.SetPart(part.Key, part.Value.Key, part.Value.Value);
            }
        }
    }

    public void LoadAvatarParts()
    {
        if (Avatar == null)
            return;
        Avatar.LoadParts();
    }
    public void SetPlayerShader(GameObject go)
    {
        Shader sha = GameGlobal.Instance.getShader("King/Character/ToonCharacter+XRay");
        Texture tex;
        Material mat;
        foreach (var o in go.GetComponentsInChildren<Renderer>(true))
        {
            if (o.material.shader.name == "King/Character/ToonCharacter")
            {
                mat = new Material(sha);
                tex = o.material.GetTexture("_color_map");
                mat.SetTexture("_color_map", tex);
                mat.SetColor("_XRayColor", new Color(0.2f,0.2f,1,1));
                o.material = mat;
            }
        }
    }
    public void SetOtherPlayerShader(GameObject go)
    {
        foreach (var o in go.GetComponentsInChildren<Renderer>(true))
        {
            //o.material.shader = Shader.Find("King/Character/ToonCharacter");
        }
    }
    #endregion

    private float AnimationDelta;
    private const int AnimationHistoryCount = 5;
    private List<AnimationInfo> AnimationList = new List<AnimationInfo>();
    //模型.
    private Stack<GameObject> GameCharactorStack = new Stack<GameObject>();
    //维护一个播放历史.
    private Stack<AnimationInfo> AnimationHistory = new Stack<AnimationInfo>(AnimationHistoryCount);


    public RenderUnit(string name = "RenderUnit", bool isPlayer = false)
    {
        IsActor = false;
        this.IsPlayer = isPlayer;
        init(name);

    }

    public void setAssetBoundleLoader(AssetBoundleLoader load)
    {
        this.mAssetloader = load;
    }
    private void init(string name = "RenderUnit")
    {
        //根节点.
        if (root == null)
        {
            root = new GameObject();
            root.name = "RenderUnitRoot";
            root.transform.position = new Vector3(10000, 10000, 10000);
        }

        this.AnimationDelta = -1;
        this.IsDestroy = false;
        this.RootObject = new GameObject();
        this.RootObject.name = name;
        RootObject.transform.parent = root.transform;
        this.RootObject.transform.localPosition = Vector3.zero;

        //挂载特效节点.
        EffectNode = new GameObject();
        EffectNode.name = "EffectNode";
        EffectNode.transform.parent = RootObject.transform;
        EffectNode.transform.localPosition = Vector3.zero;
        EffectNode.transform.localScale = Vector3.one;
        EffectNode.transform.localRotation = Quaternion.identity;
    }
    ~RenderUnit()
    {
        //GameDebug.Log("------------ ~RenderUnit -------------");
    }
    public float getDefaultAnimationTime()
    {
        if (GameCharactorStack == null || GameCharactorStack.Count == 0 || GameCharactorStack.Peek() == null)
        {
            return 0;
        }
        Animation animation = this.GameCharactorStack.Peek().GetComponent<Animation>();
        if (animation.clip == null)
        {
            return 0;
        }
        return animation.clip.length;
    }
    public bool getDefaultAnimatorTimeOver()
    {
        if (GameCharactorStack == null || GameCharactorStack.Count == 0 || GameCharactorStack.Peek() == null)
        {
            return false;
        }
        Animator animator = this.GameCharactorStack.Peek().GetComponentInChildren<Animator>();

        if (animator != null)
        {
            float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (time < 1)
            {
                return false;
            }
        }
        return true;
    }
    #region 动画相关.
    private void ToAnimationHistory(AnimationInfo info)
    {
        if (this.AnimationHistory.Count == AnimationHistoryCount)
        {
            this.AnimationHistory.Clear();
        }
        this.AnimationHistory.Push(info);
    }

    private bool PlayAnimationNow(AnimationInfo info)
    {
        if (mLockAction != null && !info.Name.Equals(mLockAction))
        {
            return false;
        }
        if (!CheckAnimation(info.Name))
        {
            return false;
        }
        string curAnimationName = GetCurAnimationName();

        if (!string.IsNullOrEmpty(curAnimationName) && info.Name.IndexOf("death") == -1 && ((curAnimationName.IndexOf("weakidle") != -1 && info.Name.IndexOf("weakidle") != -1)
            || (curAnimationName == "f_weak" && info.Name.IndexOf("weakidle") == -1)))
        {
            return false;
        }
        //if (IsActor)
        //{
        //    GameObject obj= this.GameCharactorStack.Peek();
        //    GameDebug.Log("playAnimation=" + info.Name);
        //}
        Animation animation = this.GameCharactorStack.Peek().GetComponent<Animation>();
        AnimationState state = animation[info.Name];
        ToAnimationHistory(info);

        state.time = 0;
        state.speed = info.Speed;
        state.wrapMode = info.Mode;
        state.layer = 1;
        if (info.Name.IndexOf("n_run") != -1 || info.Name.IndexOf("idle") != -1 || info.Name.IndexOf("weakidle") != -1 || info.Name.IndexOf("stun") != -1 || info.Name.IndexOf("death") != -1 || info.Name.IndexOf("hurt") != -1 || info.Name == mLockAction || info.Name.IndexOf(ComAIUnit.IDLEANI) != -1)
        {
            animation.CrossFade(info.Name);
        }
        else
            animation.Play(info.Name, PlayMode.StopAll);
        //animation.CrossFade(info.Name);
        //this.setInvisible(info.m_bIsInvisible);


        return true;
    }
    //强制播放动作
    public bool PlayAnimationForce(string name, WrapMode mode = WrapMode.Once, float duration = -1, float speed = 1, float delay = 0, AnimationInfo.AnimationCallBack callback = null, bool isInvisible = false, bool mustCallBack = false)
    {

        if (IsDestroy)
        {
            m_bStopFrame = false;
            return false;
        }
        StopAnimationQueue();
        if (m_currentInfo != null && m_currentInfo.mMustCallBack)
        {
            m_currentInfo.CallBack();
            m_currentInfo = null;
        }
        AnimationInfo info = new AnimationInfo(name, duration, mode, speed, delay, isInvisible, mustCallBack);
        if (callback != null)
        {
            info.OnAnimationCallBack = callback;
        }
        Animation animation = this.GameCharactorStack.Peek().GetComponent<Animation>();
        AnimationState state = animation[info.Name];
        ToAnimationHistory(info);

        state.time = 0;
        state.speed = info.Speed;
        state.wrapMode = info.Mode;
        state.layer = 1;
        if (info.Name.IndexOf("n_run") != -1 || info.Name.IndexOf("idle") != -1 || info.Name.IndexOf("weakidle") != -1 || info.Name.IndexOf("stun") != -1 || info.Name.IndexOf("death") != -1 || info.Name.IndexOf("hurt") != -1 || info.Name == mLockAction || info.Name.IndexOf(ComAIUnit.IDLEANI) != -1)
        {
            animation.CrossFade(info.Name);
        }
        else
            animation.Play(info.Name, PlayMode.StopAll);
        //animation.CrossFade(info.Name);
        //this.setInvisible(info.m_bIsInvisible);


        return true;
    }

    #region 动态加载动作
    //public void AddClip(AnimationClip ac, string actionName)
    //{
    //    if (!animationMap.ContainsKey(actionName))
    //    {
    //        animationMap.Add(actionName, ac);
    //    }

    //}
    ////是否有动作
    //public bool hasAnimation(string actionName)
    //{
    //    return animationMap.ContainsKey(actionName);
    //}
    public void InitAnimation(string[] AnimationList)
    {
        if (GameCharactorStack.Count == 0)
        {
            return;
        }
        GameObject obj = this.GameCharactorStack.Peek();
        if (obj == null)
        {
            return;
        }
        this.m_AnimationNameList = AnimationList;
        InitAnimation(obj);


    }

    public void InitAnimation(List<string> AnimationList)
    {
        if (GameCharactorStack.Count == 0)
        {
            return;
        }
        GameObject obj = this.GameCharactorStack.Peek();
        if (obj == null)
        {
            return;
        }
        InitAnimation(obj, AnimationList);


    }
    public void InitAnimation(GameObject obj, List<string> AnimationSpeicalList = null)
    {
        Animation animations = obj.GetComponent<Animation>();
        if (animations == null)
        {
            animations = obj.AddComponent<Animation>();
        }
        animations.enabled = true;
        if (m_AnimationNameList != null)
        {
            for (int i = 0; i < m_AnimationNameList.Length; i++)
            {
                if (animationMap.ContainsKey(m_AnimationNameList[i]) && animationMap[m_AnimationNameList[i]] != null)
                {
                    if (animations.GetClip(m_AnimationNameList[i]) == null)
                    {
                        animations.AddClip(animationMap[m_AnimationNameList[i]], m_AnimationNameList[i]);
                        //if (m_animationnamelist[i].endswith("idle"))
                        //{
                        //    if (isfirstload)
                        //    {
                        //        playanimation(m_animationnamelist[i], wrapmode.loop);
                        //        isfirstload = false;
                        //    }
                        //}
                    }
                }
            }
        }

        if (AnimationSpeicalList != null)
        {
            for (int i = 0; i < AnimationSpeicalList.Count; i++)
            {
                if (animationMap.ContainsKey(AnimationSpeicalList[i]) && animationMap[AnimationSpeicalList[i]] != null)
                {
                    if (animations.GetClip(AnimationSpeicalList[i]) == null)
                    {
                        animations.AddClip(animationMap[AnimationSpeicalList[i]], AnimationSpeicalList[i]);
                    }
                }
            }
        }

        //foreach (KeyValuePair<string, AnimationClip> p in animationMap)
        //{

        //    string uiTag = p.Key;
        //    if (animationMap.ContainsKey(uiTag) && animationMap[uiTag] != null)
        //    {
        //        if (animations.GetClip(uiTag) == null)
        //        {
        //            if (animationMap[uiTag] != null && !string.IsNullOrEmpty(uiTag))
        //            {
        //                animations.AddClip(animationMap[uiTag], uiTag);
        //                if (uiTag.EndsWith("f_idle"))
        //                {
        //                    if (isFirstLoad)
        //                    {
        //                        PlayAnimation(uiTag, WrapMode.Loop);
        //                        isFirstLoad = false;
        //                    }
        //                }        
        //            }
        //            else
        //            {
        //                Debuger.Log("animationMap[uiTag]= " + uiTag + " can not be loaded");
        //            }
        //        }

        //    }
        //}
        //FakeMotionBlur fmb = this.GameCharactorStack.Peek().GetComponent<FakeMotionBlur>();
        //if (fmb == null)
        //{
        //    fmb = this.GameCharactorStack.Peek().AddComponent<FakeMotionBlur>();
        //}
        //PlayAnimation("f_idle", WrapMode.Loop);
        m_Fcm = this.GameCharactorStack.Peek().GetComponent<FresnelControlManager>();
        if (m_Fcm == null)
        {
            m_Fcm = this.GameCharactorStack.Peek().AddComponent<FresnelControlManager>();
        }
    }
    #endregion
    /// <summary>
    /// 是否有这个动画.
    /// </summary>
    /// <returns></returns>
    public bool CheckAnimation(string name)
    {
        if (this.GameCharactorStack.Count == 0)
        {
            return false;
        }

        GameObject obj = this.GameCharactorStack.Peek();
        if (obj == null)
        {
            return false;
        }

        Animation animation = obj.GetComponent<Animation>();
        if (animation == null)
        {
            return false;
        }
        AnimationState state = animation[name];
        if (state == null)
        {
            return false;
        }
        if (GetCurAnimationName() == name)
        {
            return false;
        }

        return true;
    }

    public int GetFrameCount(string name)
    {
        if (this.GameCharactorStack.Count == 0)
        {
            return 0;
        }
        try
        {
            AnimationState state = this.GameCharactorStack.Peek().GetComponent<Animation>()[name];
            if (state == null)
            {
                return 0;
            }
            else
            {
                return (int)(state.length * 1000);
            }

        }
        catch (Exception e)
        {
            GameDebug.Log("GetFrameCountName=" + name);
            return 0;
        }



    }

    /// <summary>
    /// 动画是否播放中.
    /// </summary>
    /// <returns></returns>
    public bool IsPlaying()
    {
        if (this.GameCharactorStack.Count == 0)
        {
            return false;
        }
        return this.GameCharactorStack.Peek().GetComponent<Animation>().isPlaying;
    }
    //这里几个getcomponent需要优化
    /// <summary>
    /// 指定动画是否播放中.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool IsPlaying(string name)
    {
        if (this.GameCharactorStack.Count == 0)
        {
            return false;
        }
        if (string.IsNullOrEmpty(name))
        {
            return true;
        }
        GameObject peekObj = this.GameCharactorStack.Peek();
        Animation anim = peekObj.GetComponent<Animation>();
        if (anim == null)
        {
            return false;
        }
        return anim.IsPlaying(name) && anim[name].normalizedTime < 1.0f && !name.Equals("f_idle");
    }

    public void StopPlay(string name)
    {
        if (this.GameCharactorStack.Count > 0 && !string.IsNullOrEmpty(name))
        {
            GameObject peekObj = this.GameCharactorStack.Peek();
            Animation anim = peekObj.GetComponent<Animation>();
            if (anim != null)
            {
                anim.Stop(name);
            }
        }
    }

    /// <summary>
    /// 获取当前正在播放的动画名称.
    /// </summary>
    /// <returns>正在播放返回当前播放的动画名称,否则返回null</returns>
    public string GetCurAnimationName()
    {
        if (this.AnimationHistory.Count == 0)
        {
            return null;
        }
        if (IsPlaying(this.AnimationHistory.Peek().Name))
        {
            return this.AnimationHistory.Peek().Name;
        }
        else
        {
            return null;
        }
    }
    //定帧
    public void stopActionFrame(float speedrate)
    {
        //if (IsPlaying())
        //{
        //Animation ani = this.GameCharactorStack.Peek().animation;
        //AnimationState state = ani[this.AnimationHistory.Peek().Name];
        //state.enabled = false;
        m_bStopFrame = true;
        Time.timeScale = speedrate;
        //}
    }
    public void resumeActionFrame()
    {

        //Animation ani = this.GameCharactorStack.Peek().animation;
        //AnimationState state = ani[this.AnimationHistory.Peek().Name];
        //state.enabled = true;
        m_bStopFrame = false;
        Time.timeScale = 1f;

    }
    public void Pause()
    {
        if (IsPlaying())
        {
            Animation ani = this.GameCharactorStack.Peek().GetComponent<Animation>();
            AnimationState state = ani[this.AnimationHistory.Peek().Name];
            if (state != null)
            {
                state.speed = 0;
            }
        }
    }
    public bool hasAniamtion(string fileName)
    {
        if (this.GameCharactorStack.Count == 0 || this.GameCharactorStack.Peek() == null)
        {
            return false;
        }
        Animation ani = this.GameCharactorStack.Peek().GetComponent<Animation>();
        if (ani == null)
        {
            Debug.LogError("ani is null");
            return false;
        }
        AnimationState state = ani[fileName];
        if (state != null)
        {
            return true;
        }
        return false;
    }
    public void CancelPause()
    {
        if (IsPlaying())
        {
            Animation ani = this.GameCharactorStack.Peek().GetComponent<Animation>();
            AnimationState state = ani[this.AnimationHistory.Peek().Name];
            if (state != null)
            {
                state.speed = this.AnimationHistory.Peek().Speed;
            }
        }
    }
    public void setLockActionAnimation(string fileName)
    {
        mLockAction = fileName;
    }
    public void delLockActionAnimation(string fileName)
    {
        if (mLockAction == fileName)
        {
            mLockAction = null;
        }
    }
    public string getLockActionAnimation()
    {
        return mLockAction;
    }

    public bool PlayAnimation(string name, WrapMode mode = WrapMode.Once, float duration = -1, float speed = 1, float delay = 0, AnimationInfo.AnimationCallBack callback = null, bool isInvisible = false, bool mustCallBack = false)
    {

        if (IsDestroy)
        {
            m_bStopFrame = false;
            return false;
        }
        if (IsActor)
        {
            //Debuger.Log("name="+name);
        }
        string curAnimationName = GetCurAnimationName();
        if (curAnimationName != null && curAnimationName.Contains("n_show"))
        {
            return false;
        }
        if (name.IndexOf("death") == -1 && !string.IsNullOrEmpty(curAnimationName) && ((curAnimationName == "f_weak" && name.IndexOf("weakidle") == -1)))
        {
            return false;
        }
        if (!CheckAnimation(name))
        {
            if (callback != null)
            {
                callback.Invoke(null);
            }
            return false;
        }
        StopAnimationQueue();
        if (m_currentInfo != null && m_currentInfo.mMustCallBack)
        {
            m_currentInfo.CallBack();
            m_currentInfo = null;
        }
        AnimationInfo info = new AnimationInfo(name, duration, mode, speed, delay, isInvisible, mustCallBack);
        if (callback != null)
        {
            info.OnAnimationCallBack = callback;
        }
        AnimationList.Add(info);
        PlayAnimationQueue();

        return true;
    }

    public void AddPlayAnimation(string name, float duration,
        WrapMode mode = WrapMode.Once, float speed = 1, float delay = 0,
        AnimationInfo.AnimationCallBack callback = null, bool isInvisible = false)
    {
        if (IsDestroy) { return; }
        AnimationInfo info = new AnimationInfo(name, duration, mode, speed, delay, isInvisible);
        info.OnAnimationCallBack = callback;
        AnimationList.Add(info);
    }

    public void PlayAnimationQueue()
    {
        if (AnimationList.Count > 0)
        {

            m_bStopFrame = false;
            AnimationDelta = 0;
            AnimationInfo info = AnimationList[0];
            info.InDelayState = true;
            PlayAnimationNow(info);
            m_currentInfo = info;

        }

    }

    public void StopAnimationQueue()
    {
        AnimationList.Clear();
    }

    #endregion

    #region 坐标方向缩放等原子操作, 动态更新时, 请使用以下方法代替直接赋值.

    public void AtomEvent(AtomType type, GameObject obj)
    {
        if (_OnAtom != null)
        {
            _OnAtom.Invoke(type, obj);
        }
    }

    public void SetPosition(Vector3 pos, float elps = 0)
    {
        if (this.RootObject != null && !m_bStopFrame)
        {
            Vector3 fixpos = new Vector3();
            if (double.IsNaN(pos.x) || double.IsNaN(pos.y) || double.IsNaN(pos.z))
            {
                if (double.IsNaN(pos.x))
                {
                    fixpos.x = this.RootObject.transform.position.x;
                }
                else
                {
                    fixpos.x = pos.x;
                }
                if (double.IsNaN(pos.y))
                {
                    fixpos.y = this.RootObject.transform.position.y;
                }
                else
                {
                    fixpos.y = pos.y;
                }
                if (double.IsNaN(pos.z))
                {
                    fixpos.z = this.RootObject.transform.position.z;
                }
                else
                {
                    fixpos.z = pos.z;
                }
            }
            else
            {
                fixpos = pos;
            }
            this.RootObject.transform.position = fixpos;
            AtomEvent(AtomType.Positon, this.RootObject);
        }
    }

    public void SetDirection(Quaternion rotation)
    {
        if (this.RootObject != null)
        {
            this.RootObject.transform.rotation = rotation;
            AtomEvent(AtomType.Direction, this.RootObject);
        }
    }

    public void SetCharactorLocalScale(Vector3 s)
    {
        if (this.GameCharactorStack.Count > 0)
        {
            this.GameCharactorStack.Peek().transform.localScale = s;
            AtomEvent(AtomType.Scale, this.GameCharactorStack.Peek());
        }

    }

    #endregion
    public void PlaySkinedMeshAnimation()
    {
        if (blinkPart)
        {
            blinkPart.BlinkNow();
        }
    }
    #region 清理.
    private void DestroyCharactor()
    {
        ClearPart();

        foreach (GameObject obj in this.GameCharactorStack)
        {
            //DestroyGameObject(obj);
            obj.transform.parent = null;
            ResourceMgr.Instance.CollectUnitInstance(obj);
        }
        this.GameCharactorStack.Clear();
    }

    private void DestroyGameObject(GameObject obj)
    {
        if (obj != null)
        {
            //foreach (MeshRenderer mr in obj.GetComponentsInChildren<MeshRenderer>())
            //{
            //    if (mr.material != null)
            //    {
            //        GameObject.Destroy(mr.material);
            //        mr.material = null;
            //    }
            //}
            GameObject.Destroy(obj);

        }
    }

    private void DestroyEffects()
    {
        for (int i = 0; i < EffectNode.transform.childCount; ++i)
        {
            GameObject o = EffectNode.transform.GetChild(i).gameObject;
            UnitEffectMono eff = o.GetComponent<UnitEffectMono>();
            if (eff != null && eff.name != null)
            {
                //放回对象池.
                ResourceMgr.Instance.CollectUnitInstance(o);
                //AssetBoundleLoader.Unload(eff.name, o);
            }
        }
    }

    private void DestroyRootObject()
    {
        if (this.RootObject != null)
        {
            //DestroyEffects();
            DestroyGameObject(this.RootObject);
            this.RootObject = null;
        }
        if (this.EffectNode != null)
        {
            //DestroyEffects();
            DestroyGameObject(this.EffectNode);
            this.EffectNode = null;
        }




    }

    private bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }
        if (disposing)
        {
            //执行基本的清理代码
            Destroy();
            DestroyRootObject();

        }
        this.disposed = true;
    }

    public void Dispose()
    {
        this.Dispose(true);
        System.GC.SuppressFinalize(this);
    }

    public void Destroy()
    {
        ClearAvatar();
        clearWeapon();
        //if (file)
        //    file.Close();
        this.IsDestroy = true;
        animationMap.Clear();
        DestroyCharactor();
        m_AnimationNameList = null;
        //由于RootObject的生命周期改变（改为在构造的时候创建），所以这里Unload的时候不能删除
        //DestroyRootObject();

        //Debuger.Log("Destroy " + this.Name);
    }

    public void Destroy(float time, System.Action<GameObject> act)
    {
        if (this.RootObject != null)
        {
            if (time > 0)
            {
                DelayTimeBehaviour dt = this.RootObject.AddComponent<DelayTimeBehaviour>();
                Update();
                dt.SetDelay(time, (GameObject obj, float x) =>
                {
                    Destroy();

                    if (act != null)
                    {
                        act.Invoke(obj);
                    }
                });
            }
            else
            {
                Destroy();
                if (act != null)
                {
                    act.Invoke(this.RootObject);
                }
            }

        }

    }

    public void DestroyWithAnimation(string name, float duration,
                                    WrapMode mode = WrapMode.Once,
                                    float speed = 1,
                                    float delay = 0, bool isInvisible = true)
    {
        StopAnimationQueue();
        AnimationInfo info = new AnimationInfo(name, duration, mode, speed, delay, isInvisible);
        info.DestroyAnimation = true;
        AnimationList.Add(info);
        PlayAnimationQueue();
    }



    #endregion
    public GameObject PeekCharactor()
    {
        if (this.GameCharactorStack.Count != 0)
        {
            return this.GameCharactorStack.Peek();
        }
        return null;
    }
    /// <summary>
    /// 状态更新方法, 此方法必须在外部每帧调用一次
    /// </summary>
    /// <returns></returns>
    public void Update()
    {
        if (!m_bStopFrame && m_currentInfo != null && this.AnimationList.Count > 0)
        {
            if (this.AnimationDelta >= m_currentInfo.Duration)
            {
                if (!IsPlaying(m_currentInfo.Name))
                {
                    this.AnimationList.RemoveAt(0);
                    this.AnimationDelta = 0;
                    m_currentInfo.CallBack();
                    //if (m_currentInfo.DestroyAnimation)
                    //{
                    //    Destroy();
                    //}
                    if (this.AnimationList.Count > 0)
                    {
                        m_currentInfo = this.AnimationList[0];
                        PlayAnimationNow(m_currentInfo);
                    }
                    else
                    {
                        m_currentInfo = null;
                    }
                }

            }
            else
            {
                this.AnimationDelta += Time.deltaTime * 1000;

            }
        }
        //if(this.AnimationDelta >= 0 && this.AnimationList.Count > 0)
        //{
        //    AnimationInfo info = AnimationList[0];
        //    //如果在预播放状态(info.InDelayState为true)则执行延迟播放逻辑
        //    //否则执行动画持续时间等待
        //    this.AnimationDelta += Time.deltaTime * 1000;

        //    if (info.InDelayState)
        //    {
        //        if (this.AnimationDelta >= info.DelayTime)
        //        {
        //            Debuger.Log("time=" + AnimationDelta + "DelayTime=" + info.DelayTime);
        //            PlayAnimationNow(info);
        //            info.InDelayState = false;
        //            this.AnimationDelta = 0;
        //        }
        //    }
        //    else if (this.AnimationDelta >= info.Duration)
        //    {
        //        //if (!IsPlaying(m_currentSkillName))
        //        //{
        //              info.CallBack();
        //             this.AnimationList.RemoveAt(0);
        //             this.AnimationDelta = 0;
        //             if (info.DestroyAnimation)
        //             {
        //                 Destroy();
        //             }
        //             if (this.AnimationList.Count > 0)
        //             {
        //                 this.AnimationList[0].InDelayState = true;
        //             }
        //        //}

        //    }
        //}
        //else
        //{
        //    this.AnimationDelta = -1;
        //}
        ////test 测试残影
        //if (GetCurAnimationName() == "f_run")
        //{
        //    if(m_time >= 0.2){
        //        FakeMotionBlur fmb = this.GameCharactorStack.Peek().GetComponent<FakeMotionBlur>();
        //        if (fmb != null)
        //        {
        //            fmb.showShadow();
        //            m_time = 0;
        //        }

        //    }
        //    else
        //    {
        //        m_time += Time.deltaTime;
        //    }

        //}


    }

    /// <summary>
    /// 加入模型.
    /// </summary>
    /// <param name="obj"></param>
    public void PushCharactor(GameObject obj, bool isInitAnimation = true)
    {
        bool isshowDebug = false;
        if(isshowDebug)
        Debuger.Log("pushChar = 0 " + obj.name);
        if (this.RootObject != null)
        {
            this.RootObject.name = obj.name;
            if (this.RootObject.transform.parent == root.transform)
            {
                this.RootObject.transform.parent = null;
            }
            obj.transform.parent = this.RootObject.transform;
            obj.transform.position = this.RootObject.transform.position;
        }
        else
        {
            obj.transform.position = Vector3.zero;
        }
        if (isshowDebug)
            Debuger.Log("pushChar = 1 " + obj.name);

        obj.transform.rotation = Quaternion.identity;
        obj.transform.localEulerAngles = Vector3.zero;
        if (isshowDebug)
            Debuger.Log("pushChar = 2 " + obj.name);
        if (GameCharactorStack.Count != 0)
        {
            GameCharactorStack.Peek().SetActive(false);
        }
        if (isshowDebug)
            Debuger.Log("pushChar = 3 " + obj.name);
        GameCharactorStack.Push(obj);

        if (isshowDebug)
            Debuger.Log("pushChar = 4 " + obj.name);
        if (isInitAnimation)
        {
            FindPart(obj);
            InitAnimation(obj);
        }
        if (isshowDebug)
            Debuger.Log("pushChar = 5 " + obj.name);
        InitRendererCache(obj);
        if (isshowDebug)
            Debuger.Log("pushChar = 6 " + obj.name);
        if (IsPlayer)
        {
            initAlwaysAnimate();
        }
        if (isshowDebug)
            Debuger.Log("pushChar = 7 " + obj.name);
        #region Avatar相关，暂未开启.

        //设置Avatar Body信息
        if (Avatar != null)
        {
            Avatar.Body.PartObj = obj;
            Avatar.Body.IsDirt = false;
        }

        #endregion
        if (isshowDebug)
            Debuger.Log("pushChar = 8 " + obj.name);

        SkinnedMeshRenderer[] mSkinrenders = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int i = 0; i < mSkinrenders.Length; i++)
        {
            SkinnedMeshRenderer skin = mSkinrenders[i];
            if (skin != null && skin.materials != null)
            {
                for (int j = 0; j < skin.materials.Length; j++)
                {
                    if (skin.materials[j] != null)
                    {
                        Material _material = new Material(skin.materials[j]);
                        string name = skin.materials[j].name.Replace(" (Instance)", "");
                        if (!mMaterialMap.ContainsKey(name))
                        {

                            mMaterialMap.Add(name, _material);
                        }
                    }

                }
            }
        }
        if (isshowDebug)
            Debuger.Log("pushChar = 9 " + obj.name);

    }

    #region 缓存renderer
    private void InitRendererCache(GameObject obj)
    {
        Transform[] allTrans = obj.GetComponentsInChildren<Transform>();
        for (int i = 0; i < allTrans.Length; i++)
        {
            if (allTrans[i].gameObject.GetComponent<Renderer>() != null)
            {
                mRenderList.Add(allTrans[i].gameObject.GetComponent<Renderer>());
            }
        }
    }
    #endregion
    //角色数
    public int CharactorCount()
    {
        if (this.GameCharactorStack == null)
        {
            return 0;
        }
        return this.GameCharactorStack.Count;
    }
    /// <summary>
    /// 推出栈顶模型, 可用于换装结束后的还原处理.
    /// </summary>
    /// <returns></returns>
    public void PopCharactor()
    {
        ClearPart();
        mRenderList.Clear();
        if (this.GameCharactorStack.Count == 0)
        {
            return;
        }
        GameObject obj = this.GameCharactorStack.Pop();
        DestroyGameObject(obj);
        if (this.GameCharactorStack.Count > 0)
        {
            this.GameCharactorStack.Peek().SetActive(true);
            InitRendererCache(this.GameCharactorStack.Peek());
            //OnEnterState(null);
        }
        else
        {
            DestroyGameObject(this.RootObject);
            this.RootObject = null;
        }
    }
    //寻找关键挂载点.
    public void FindPart(GameObject obj)
    {
        if (obj == null) { return; }
        mPartMap.Clear();
        Transform[] tfs = obj.GetComponentsInChildren<Transform>();
        string temp = null;
        foreach (Transform t in tfs)
        {
            temp = t.name;
            if (temp == PART_HEAD_BUFF)
            {
                mPartMap.Add(PART_HEAD_BUFF, t.gameObject);
            }
            else if (temp == PART_FOOT_BUFF)
            {
                mPartMap.Add(PART_FOOT_BUFF, t.gameObject);
            }
            else if (temp == PART_CHEST_BUFF)
            {
                mPartMap.Add(PART_CHEST_BUFF, t.gameObject);
            }
            else if (temp == PART_RIGHT_HAND)
            {
                mPartMap.Add(PART_RIGHT_HAND, t.gameObject);
            }
            else if (temp == PART_LEFT_HAND)
            {
                mPartMap.Add(PART_LEFT_HAND, t.gameObject);
            }
            else if (temp == PART_FIRSTGLOVE)
            {
                mPartMap.Add(PART_FIRSTGLOVE, t.gameObject);
            }
            else if (temp == PART_LEFT_SPELL)
            {
                mPartMap.Add(PART_LEFT_SPELL, t.gameObject);
            }
            else if (temp == PART_RIGHT_SPELL)
            {
                mPartMap.Add(PART_RIGHT_SPELL, t.gameObject);
            }
            else if (temp == PART_HEAD)
            {
                mPartMap.Add(PART_HEAD, t.gameObject);
            }
            else if (temp == PART_REAR_BUFF)
            {
                mPartMap.Add(PART_REAR_BUFF, t.gameObject);
            }
            else if (temp == PART_COFFIN)
            {
                mPartMap.Add(PART_COFFIN, t.gameObject);
            }
        }
    }
    #region 武器切换（新增节点）
    public void changeWeapon(List<WeaponChangeList> list, LoadWeaponCallBack callBack = null)
    {
        if (list == null)
        {
            Debuger.Log("no WeaponChangeList");
            return;
        }
        List<GameObject> partlist = new List<GameObject>();
        foreach (WeaponChangeList wcl in list)
        {
            partlist.Add(getWeaponPart(wcl.BindPartName));
        }
        clearWeapon();
        mLoadWeaponCallBack = callBack;
        int i = 0;
        foreach (WeaponChangeList wcl in list)
        {
            AttachWeaponToTargetPart(wcl.weaponAssetFileName, partlist[i++]);

        }
    }
    public void loadChangeWeaponCallBack(GameObject obj, object userdata, bool isLoadok)
    {
        if (obj != null)
        {
            GameObject partobj = userdata as GameObject;
            if (partobj == null) { return; }

            partobj.SetActive(true);
            obj.transform.parent = partobj.transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            obj.transform.localRotation = Quaternion.identity;
            Transform[] objTrans = obj.GetComponentsInChildren<Transform>();
            for (int i = 0; i < objTrans.Length; i++)
            {
                objTrans[i].gameObject.layer = LayerMask.NameToLayer("Player");
            }

            Transform[] allTrans = obj.GetComponentsInChildren<Transform>();

            for (int i = 0; i < allTrans.Length; i++)
            {
                if (allTrans[i].gameObject.GetComponent<Renderer>() != null)
                {
                    if (!mRenderList.Contains(allTrans[i].gameObject.GetComponent<Renderer>()))
                    {
                        mRenderList.Add(allTrans[i].gameObject.GetComponent<Renderer>());
                    }

                }
            }

            MeshRenderer[] meshs = obj.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < meshs.Length; i++)
            {
                MeshRenderer _mesh = meshs[i];
                if (_mesh != null && _mesh.materials != null)
                {
                    for (int j = 0; j < _mesh.materials.Length; j++)
                    {
                        if (_mesh.materials[j] != null)
                        {
                            Material _material = new Material(_mesh.materials[j]);
                            string name = _mesh.materials[j].name.Replace(" (Instance)", "");
                            if (!mMaterialMap.ContainsKey(name))
                            {
                                mMaterialMap.Add(name, _material);
                            }
                        }

                    }
                }
            }
            if (mLoadWeaponCallBack != null)
            {
                mLoadWeaponCallBack.Invoke(obj);
            }
        }
    }
    //public void loadChangeWeaponCallBack(GameObject obj, object userdata)
    //{
    //    if (obj != null)
    //    {
    //        GameObject partobj = userdata as GameObject;
    //        if (partobj == null) { return; }

    //        partobj.SetActive(true);
    //        obj.transform.parent = partobj.transform;
    //        obj.transform.localPosition = Vector3.zero;
    //        obj.transform.localScale = Vector3.one;
    //        obj.transform.localRotation = Quaternion.identity;
    //        Transform[] objTrans = obj.GetComponentsInChildren<Transform>();
    //        for (int i = 0; i < objTrans.Length; i++)
    //        {
    //            objTrans[i].gameObject.layer = LayerMask.NameToLayer("Player");
    //        }

    //        Transform[] allTrans = obj.GetComponentsInChildren<Transform>();

    //        for (int i = 0; i < allTrans.Length; i++)
    //        {
    //            if (allTrans[i].gameObject.GetComponent<Renderer>() != null)
    //            {
    //                if (!mRenderList.Contains(allTrans[i].gameObject.GetComponent<Renderer>()))
    //                {
    //                    mRenderList.Add(allTrans[i].gameObject.GetComponent<Renderer>());
    //                }

    //            }
    //        }

    //                MeshRenderer[] meshs = obj.GetComponentsInChildren<MeshRenderer>();
    //                for (int i = 0; i < meshs.Length; i++)
    //                {
    //                    MeshRenderer _mesh = meshs[i];
    //                    if (_mesh != null && _mesh.materials != null)
    //                    {
    //                        for (int j = 0; j < _mesh.materials.Length; j++)
    //                        {
    //                            if (_mesh.materials[j] != null)
    //                            {
    //                                Material _material = new Material(_mesh.materials[j]);
    //                                string name = _mesh.materials[j].name.Replace(" (Instance)", "");
    //                                if (!mMaterialMap.ContainsKey(name))
    //                                {
    //                                    mMaterialMap.Add(name, _material);
    //                                }
    //                            }

    //                        }
    //                    }
    //                }
    //    }
    //}
    private void AttachWeaponToTargetPart(string weaponFileName, GameObject partobj)
    {

        //string resName = Path.GetFileNameWithoutExtension(weaponFileName);
        //this.mAssetloader.Load(weaponFileName, resName, loadChangeWeaponCallBack, partobj);
        string resName = Path.GetFileNameWithoutExtension(weaponFileName);
        UnitLoader load = new UnitLoader(weaponFileName, resName, loadChangeWeaponCallBack, partobj);
        // ResourceMgr.Instance.RequestUnitInstance(weaponFileName, loadChangeWeaponCallBack, partobj);

    }
    public void clearWeapon()
    {
        if (mWeaponPartMap.Count == 0)
        {
            return;
        }
        foreach (string key in mWeaponPartMap.Keys)
        {
            for (int i = 0; i < mWeaponPartMap[key].transform.childCount; i++)
            {
                GameObject go = mWeaponPartMap[key].transform.GetChild(i).gameObject;
                Renderer[] render = go.GetComponentsInChildren<Renderer>();
                //for (int j = 0; j < render.Length;j++ )
                //{
                //    if (render[j] && mRenderList.Contains(render[j]))
                //    {
                //        mRenderList.Remove(render[j]);
                //    }
                //}
                ResourceMgr.Instance.CollectUnitInstance(go);
                //GameObject.DestroyImmediate(go);
            }
        }
    }
    public GameObject getWeaponPart(string partname)
    {
        GameObject obj = null;
        mWeaponPartMap.TryGetValue(partname, out obj);
        if (obj == null)
        {
            GameObject gameobj = PeekCharactor();
            if (gameobj == null)
            {
                return null;
            }
            Transform[] tfs = gameobj.GetComponentsInChildren<Transform>();
            string temp = null;
            foreach (Transform t in tfs)
            {
                temp = t.name;
                if (temp == partname)
                {
                    mWeaponPartMap.Add(partname, t.gameObject);
                    obj = t.gameObject;
                    break;
                }
            }
        }
        return obj;
    }
    #endregion
    //清理挂载part.
    private void ClearPart()
    {
        //if (mPartMap != null)
        //{
        //    mPartMap.Clear();
        //}
        //if (mWeaponPartMap != null)
        //{
        //    mWeaponPartMap.Clear();
        //}
    }
    /// <summary>
    /// 获得指定挂载点.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public GameObject GetTargetPart(string name)
    {
        if (mPartMap == null) { return null; }
        GameObject obj = null;
        mPartMap.TryGetValue(name, out obj);
        return obj;
    }
    /// <summary>
    /// 挂载到指定节点下.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool AttachToTargetPart(string name, GameObject obj)
    {
        if (obj == null) { return false; }
        GameObject o = GetTargetPart(name);
        if (o == null) { return false; }

        obj.transform.parent = o.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;
        obj.transform.localRotation = Quaternion.identity;

        return true;
    }
    //设置隐藏
    public void setIsShow(bool isShow)
    {
        m_IsShow = isShow;
    }
    #region 隐身

    public void setInvisible(bool isVisible)
    {
        GameObject obj = PeekCharactor();
        if (obj == null)
        {
            return;
        }
        if (!m_IsShow)
        {
            isVisible = true;
        }
        for (int i = 0; i < this.mRenderList.Count; i++)
        {
            if (mRenderList[i] != null)
                mRenderList[i].enabled = !isVisible;
        }
        /*if(isVisible){
            Transform[] allTrans = obj.GetComponentsInChildren<Transform>();
            for (int i = 0; i < allTrans.Length; i++)
            {
                allTrans[i].gameObject.layer = LayerMask.NameToLayer("hidelayer");
            }
        }
        else
        {
            Transform[] allTrans = obj.GetComponentsInChildren<Transform>();
            for (int i = 0; i < allTrans.Length; i++)
            {
                allTrans[i].gameObject.layer = LayerMask.NameToLayer("Player");
            }
        }*/
        //   obj.SetActive(!isVisible);
        //   obj.renderer.enabled = isVisible;

    }
    #endregion

    #region 菲尼尔效果
    public void hurtEffect()
    {
        if (m_Fcm != null)
        {
            m_Fcm.AddFresnel(0.5f, 0f, 1f, FresnelControlManager.StateType.hurt, FresnelControlManager.Fresnel.FresnelActType.immediately, 0.1f);
        }
    }
    public void NoneBlockEffect()
    {
        if (m_Fcm != null)
        {
            m_Fcm.AddFresnel(0.15f, 0f, 0.5f, FresnelControlManager.StateType.BaTi, FresnelControlManager.Fresnel.FresnelActType.immediately);
        }
    }
    public void LigthedNoEffect(float time)
    {
        if (m_Fcm != null)
        {
            m_Fcm.AddFresnel(time, 0f, 0.5f, FresnelControlManager.StateType.LightedNoEffect);
        }
    }
    public void BreakDefNoEffect(float time)
    {
        if (m_Fcm != null)
        {
            m_Fcm.AddFresnel(time, 0f, 0.5f, FresnelControlManager.StateType.BreakDefNoEffect);
        }
    }

    private FresnelControlManager.Fresnel mHong;
    public void BianHong(float time)
    {
        if (m_Fcm != null)
        {
            m_Fcm.AddFresnel(time, 0f, 0.5f, FresnelControlManager.StateType.BianHong);
        }
    }

    private FresnelControlManager.Fresnel mLan;
    public void BianLan(float time)
    {
        if (m_Fcm != null)
        {
            m_Fcm.AddFresnel(time, 0f, 0.5f, FresnelControlManager.StateType.BianLan);
        }
    }

    public void BianBai()
    {
        if (m_Fcm != null)
        {
            if (mHong != null)
            {
                m_Fcm.RemoveFresnelList(mHong);
            }
            if (mLan != null)
            {
                m_Fcm.RemoveFresnelList(mLan);
            }
        }
    }

    public void ClickDownNPC()
    {
        if (m_Fcm != null)
        {
            m_Fcm.AddKeepFresnel();
        }
    }

    public void ClickUpNPC()
    {
        if (m_Fcm != null)
        {
            m_Fcm.RemoveKeepFresnel();
        }
    }
    #endregion

    #region 玩家强制渲染显示初始化
    public void initAlwaysAnimate()
    {
        GameObject obj = PeekCharactor();
        if (obj == null)
        {
            return;
        }
        Animation animation = obj.GetComponent<Animation>();
        if (animation != null)
        {
            animation.cullingType = AnimationCullingType.AlwaysAnimate;
        }

    }
    #endregion

    #region 玩家金身效果
    public void ShowGoldBody(bool show)
    {
        GameObject obj = PeekCharactor();
        if (obj == null)
        {
            return;
        }
        if (m_Fcm != null)
        {
            m_Fcm.bLock = show;
        }

        GameObject goldBodyobj = GameObject.Find("GoldBody");
        GoldBodyMaterialScript matscript = goldBodyobj.GetComponent<GoldBodyMaterialScript>();
        if (matscript != null)
        {
            SkinnedMeshRenderer[] mSkinrenders = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < mSkinrenders.Length; i++)
            {
                SkinnedMeshRenderer skin = mSkinrenders[i];
                if (skin != null && skin.materials != null)
                {
                    Material[] materials = new Material[skin.materials.Length];
                    for (int j = 0; j < skin.materials.Length; j++)
                    {
                        string name = skin.materials[j].name.Replace(" (Instance)", "");
                        if (show)
                        {
                            materials[j] = new Material(matscript.mat);
                            materials[j].name = name;
                        }
                        else
                        {
                            Material material;
                            if (mMaterialMap.TryGetValue(name, out material))
                            {
                                materials[j] = new Material(material);
                                materials[j].name = name;
                            }

                        }

                    }
                    skin.materials = materials;

                }
            }
        }

        foreach (string weaponname in GoldenWeaponNodeList)
        {
            GameObject _obj;
            if (mPartMap.TryGetValue(weaponname, out _obj))
            {
                MeshRenderer[] meshs = _obj.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < meshs.Length; i++)
                {
                    MeshRenderer _mesh = meshs[i];
                    if (_mesh != null && _mesh.materials != null)
                    {
                        Material[] materials = new Material[_mesh.materials.Length];
                        for (int j = 0; j < _mesh.materials.Length; j++)
                        {
                            string name = _mesh.materials[j].name.Replace(" (Instance)", "");
                            if (show)
                            {
                                materials[j] = new Material(matscript.mat);
                                materials[j].name = name;
                            }
                            else
                            {
                                Material material;
                                if (mMaterialMap.TryGetValue(name, out material))
                                {
                                    materials[j] = new Material(material);
                                    materials[j].name = name;
                                }

                            }

                        }
                        _mesh.materials = materials;

                    }
                }


            }
        }



    }
    #endregion

    public delegate void LoadWeaponCallBack(GameObject obj);
    private LoadWeaponCallBack mLoadWeaponCallBack = null;

    #region 时装

    private UnitAvatar mAvatarLoader;

    public void SetAvatar(UnitAvatar loader)
    {
        mAvatarLoader = loader;
    }

    private void ClearAvatar()
    {

        if (mMaterialMap != null)
        {
            mMaterialMap.Clear();
        }
        if (mAvatarLoader != null)
        {
            mAvatarLoader.UnLoadPart();
            mAvatarLoader = null;
        }
    }

    public void PopAvatar()
    {
        if (this.GameCharactorStack.Count > 0)
        {
            GameObject obj = this.GameCharactorStack.Pop();
            DestroyGameObject(obj);
        }
        mRenderList.Clear();
    }

    #endregion
}