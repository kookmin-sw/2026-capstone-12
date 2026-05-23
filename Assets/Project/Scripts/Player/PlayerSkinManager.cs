using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 플레이어 스폰 시 Room CustomProperties["SkinIndex"]를 읽어 로봇 머티리얼을 교체한다.
/// Player.prefab 루트에 붙여 사용한다.
/// </summary>
public class PlayerSkinManager : MonoBehaviourPun
{
    // 순서: 0=Black, 1=Blue, 2=Camo1, 3=Camo2, 4=Green, 5=Red, 6=White
    [SerializeField] private Material[] skinMaterials;

    public const string SKIN_KEY = "SkinIndex";

    private struct SkinRendererData
    {
        public SkinnedMeshRenderer renderer;
        public int[] skinSlots;
    }
    private List<SkinRendererData> skinRenderers = new List<SkinRendererData>();

    void Start()
    {
        CacheSkinRenderers();
        ApplySkin();
    }

    public void ApplySkin()
    {
        int skinIndex = 0;
        if (PhotonNetwork.InRoom &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SKIN_KEY, out object val))
            skinIndex = System.Convert.ToInt32(val);

        ApplySkinIndex(skinIndex);
    }

    public void ApplySkinIndex(int index)
    {
        if (skinMaterials == null || index < 0 || index >= skinMaterials.Length) return;
        Material targetMat = skinMaterials[index];

        foreach (var data in skinRenderers)
        {
            if (data.renderer == null) continue;
            Material[] mats = data.renderer.sharedMaterials;
            foreach (int slot in data.skinSlots)
                if (slot < mats.Length) mats[slot] = targetMat;
            data.renderer.sharedMaterials = mats;
        }
    }

    void CacheSkinRenderers()
    {
        if (skinMaterials == null) return;

        var skinMatNames = new HashSet<string>();
        foreach (var mat in skinMaterials)
            if (mat != null) skinMatNames.Add(mat.name);

        foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var shared = r.sharedMaterials;
            var slots = new List<int>();
            for (int i = 0; i < shared.Length; i++)
                if (shared[i] != null && skinMatNames.Contains(shared[i].name))
                    slots.Add(i);
            if (slots.Count > 0)
                skinRenderers.Add(new SkinRendererData { renderer = r, skinSlots = slots.ToArray() });
        }
    }
}
