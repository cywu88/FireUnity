using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CommonUnity3D.MFUnity.LoadUtil;
using TianyuCommon.Plugins;




public class AvatarPart
{
    public string Slot { get; set; }

    public string PartFileName { get; set; }
    public string PartName { get; set; }
    public string DefaultPartName { get; set; }

    public GameObject DefaultPartObj { get; set; }
    public GameObject PartObj { get; set; }

    public bool IsDirt = false;

    public bool IsAccessory = false;

    public AvatarPart(string slot, string part, string partFileName, 
        string defaultPart = "")
    {
        Slot = slot;
        PartName = part;
        PartFileName = partFileName;
        DefaultPartName = defaultPart;
        IsDirt = true;
        IsAccessory = AvatarSuit.IsAccessory(PartName);
    }
}

public class AvatarSuit
{
    public AvatarPart Body;
    public Dictionary<string, AvatarPart> m_PartMap;

    Dictionary<SkinnedMeshRenderer, List<string>> m_subBones = new Dictionary<SkinnedMeshRenderer, List<string>>();

    private AssetBoundleLoader Loader;
    public AvatarSuit(string bodyFileName, string bodyName, AssetBoundleLoader loader = null)
    {
        this.m_PartMap = new Dictionary<string, AvatarPart>();
        this.Body = new AvatarPart("Avatar_Body", bodyName, bodyFileName);
        this.Loader = loader;
    }

    public void SetAvatarData(List<TianyuAvatarInfo> avatar)
    {
        foreach (var part in avatar)
        {
            //if (part.FileName != GetNowPartFileName(part.PartTag.ToString()))
            //{
            //    if (part.FileName != "")
            //    {
            //        string resName = System.IO.Path.GetFileNameWithoutExtension(part.FileName);
            //        SetPart(part.PartTag.ToString(), resName, part.FileName, part.DefaultName);
            //    }
            //    else
            //    {
            //        UnloadPart(part.PartTag.ToString());
            //    }
            //}
        }
    }

    public void LoadParts()
    {
        foreach (var part in this.m_PartMap)
        {
            //if (AvatarSuit.IsAccessory(part.Value.PartName))
            //{
            //    Loader.Load(part.Value.PartFileName, part.Value.PartName, PartLoadBack, part.Key);
            //}
            //else
            //{
                Loader.Load(part.Value.PartFileName, part.Value.PartName, PartLoadBack, part.Key);
            //}
        }
    }

    public static bool IsAccessory(string name)
    {
        if (name.Length < 4) return false;
        return name.Substring(0, 3) == "zs_";
    }

    public void ChangeBody(string bodyFileName, string bodyName)
    {
        this.Body.PartFileName = bodyFileName;
        this.Body.PartName = bodyName;
        this.Body.IsDirt = true;
        GameObject.Destroy(this.Body.PartObj);
    }

    public string GetNowPartFileName(string partName)
    {
        if (partName == "Avatar_Body" && this.Body != null)
            return this.Body.PartFileName;
        else if (this.m_PartMap.ContainsKey(partName))
            return this.m_PartMap[partName].PartFileName;
        else
            return "";
    }

    public void SetPart(string slot, string part, string partFileName, string defaultPart = "")
    {
        if (!this.m_PartMap.ContainsKey(slot))
        {
            this.m_PartMap[slot] = new AvatarPart(slot, part, partFileName, defaultPart);
        }
        else
        {
            this.m_PartMap[slot].PartName = part;
            this.m_PartMap[slot].PartFileName = partFileName;
            this.m_PartMap[slot].DefaultPartName = defaultPart;
            this.m_PartMap[slot].IsDirt = true;
            this.m_PartMap[slot].IsAccessory = AvatarSuit.IsAccessory(part);
        }
    }
    public static Transform GetPart(Transform t, string searchName)
    {
        searchName = searchName.ToLower();
        foreach (Transform c in t)
        {
            string partName = c.name.ToLower();
            //枚举不能用空格，所以在这里偷偷替换成底杠，华仔说这么干的
            partName = partName.Replace(" ", "_");

            if (partName == searchName)
            {
                return c;
            }
            else
            {
                Transform r = GetPart(c, searchName);
                if (r != null)
                {
                    return r;
                }
            }
        }
        return null;
    }

    private void HookPart(string partName, GameObject avatarModel)
    {
        // 需要替换的部件
        Transform avatarPart = avatarModel.transform;
        //Transform bonePart = GetPart(avatarPart, partName);
        if (avatarPart == null)
        {
            Debuger.LogError(string.Format("Avatar Part Not Found: ", partName));
            return;
        }

        Transform bodyPart = GetPart(this.Body.PartObj.transform, partName);

        // 设置到body上的新物件
        avatarPart.transform.parent = bodyPart;
        avatarPart.transform.localPosition = Vector3.zero;
        avatarPart.transform.localRotation = Quaternion.identity;
        // avatarPart.transform.localPosition = avatarPart.InverseTransformPoint(bonePart.position);
        // avatarPart.transform.forward = avatarPart.TransformDirection(avatarPart.InverseTransformDirection(bonePart.forward));
        avatarPart.transform.localScale = Vector3.one;

    }

    private void CheckAndCreateModel()
    {
        if (this.Body.IsDirt)
            return;
        foreach (var item in this.m_PartMap)
        {
            if (item.Value.IsDirt)
            {
                return;
            }
        }
        this.m_subBones.Clear();
        foreach (var item in this.m_PartMap)
        {
            AvatarPart avatarPart = item.Value;
            if (avatarPart.PartFileName != "" && !avatarPart.IsAccessory)
                InitBones(avatarPart.PartObj); 
        }
        InitBones(this.Body.PartObj);
        CreateModel();
        foreach (var item in this.m_PartMap)
        {
            if (item.Value.PartFileName != "" && item.Value.IsAccessory)
                HookPart(item.Key, item.Value.PartObj);
        }
    }

    public void ChangePart(string partName, GameObject avatarModel)
    {
        //InitBones(avatarModel); 
        //InitBones(Body.PartObj);
        //CreateModel();
        
        // 先卸载当前部件
        AvatarPart currentInfo;
        if (this.m_PartMap.TryGetValue(partName, out currentInfo))
        {
            if (currentInfo.PartObj != null)
            {
                //GameObject.Destroy(currentInfo.PartObj);
                currentInfo.PartObj = null;
            }

            //if (currentInfo.defaultPart != null)
            //{
            //    currentInfo.defaultPart.SetActive(true);
            //}
        }
        //if (AvatarSuit.IsAccessory(currentInfo.PartName))
        //    currentInfo.PartObj = (GameObject)GameObject.Instantiate(avatarModel);
        //else
            currentInfo.PartObj = avatarModel;

        currentInfo.IsDirt = false;
        CheckAndCreateModel();
        
    }

    public void InitBones(GameObject part)
    {
        foreach (SkinnedMeshRenderer smr in part.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            List<string> boneNames = new List<string>();
            foreach (Transform t in smr.bones)
                boneNames.Add(t.name);
            this.m_subBones.Add(smr, boneNames);
        }
    }

    SkinnedMeshRenderer GetSkinnedMeshRenderer(SkinnedMeshRenderer element)
    {
        return element;
    }
    public GameObject CreateModel()
    {
        //创建一套只有骨骼的实例
        GameObject root = this.Body.PartObj;
        List<CombineInstance> t_combineInstances = new List<CombineInstance>();
        List<Material> t_materials = new List<Material>();
        List<Transform> t_bones = new List<Transform>();
        //获取所有骨架对象
        Transform[] transforms = root.GetComponentsInChildren<Transform>();
        //开始配置每一个部件
        foreach (var item in this.m_subBones)
        {
            SkinnedMeshRenderer element = item.Key;
            //创建对应的子模型
            SkinnedMeshRenderer smr = GetSkinnedMeshRenderer(element);
            for (int sub = 0; sub < smr.sharedMesh.subMeshCount; sub++)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = smr.sharedMesh;
                ci.subMeshIndex = sub;
                t_combineInstances.Add(ci);
            }
            //加入材质
            t_materials.AddRange(smr.materials);
            //将骨骼对象按源序列排列
            //foreach (string bone in this.m_subBones[element])
            foreach (string bone in item.Value)
            {
                foreach (Transform transform in transforms)
                {
                    if (transform.name != bone)
                        continue;
                    t_bones.Add(transform);
                    break;
                }
            }
        }
        //combineInstances bones materials
        //网格，骨骼，材质，全部对应上了后，合并网格并将骨骼和材质对应上
        //如果第二个参数为true，所有的网格会被结合成一个单个子网格。
        //否则每一个网格都将变成单个不同的子网格。如果所有的网格共享同一种材质，
        //设定它为真。如果第三个参数为false，在CombineInstance结构中的变换矩阵将被忽略。
        SkinnedMeshRenderer r = root.GetComponentInChildren<SkinnedMeshRenderer>();
        if (r == null)//如果没有，则给他创建一个
            r = (SkinnedMeshRenderer)root.AddComponent(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
        r.sharedMesh = new Mesh();
        r.sharedMesh.CombineMeshes(t_combineInstances.ToArray(), false, false);
        r.bones = t_bones.ToArray();
        r.materials = t_materials.ToArray();
        return root;
    } 

    void SetBones(GameObject avatarPart, GameObject bodyPart)
    {
        //SkinnedMeshRenderer render = avatarPart.GetComponentInChildren<SkinnedMeshRenderer>();
        //Lisr
        //render.bones;
    }

    public void UnloadPart(string partName)
    {
        if (AvatarSuit.IsAccessory(this.m_PartMap[partName].PartName))
            GameObject.Destroy(this.m_PartMap[partName].PartObj);
        this.m_PartMap[partName].PartName = "";
        this.m_PartMap[partName].PartFileName = "";
        this.m_PartMap[partName].PartObj = null;
        
    }

    public void PartLoadBack(GameObject o, object userdata,bool isloadOk)
    {
        string partName = userdata.ToString();
        ChangePart(partName, o);
    }
}

    

    
