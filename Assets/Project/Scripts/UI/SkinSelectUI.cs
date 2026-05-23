using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RoomScene 스킨 선택 UI.
/// - 스킨은 Room CustomProperties["SkinIndex"]에 저장 → 방 안 모두 공유
/// - 한 명이 바꾸면 OnRoomPropertiesUpdate 콜백으로 상대방 화면도 즉시 반영
/// </summary>
public class SkinSelectUI : MonoBehaviourPunCallbacks
{
    [Header("프리뷰")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform previewRobotRoot;
    [SerializeField] private float rotationSpeed = 40f;
    [SerializeField] private Vector2 renderTextureSize = new Vector2(512, 512);

    [Header("스킨 데이터")]
    // 순서: 0=Black, 1=Blue, 2=Camo1, 3=Camo2, 4=Green, 5=Red, 6=White
    [SerializeField] private Material[] skinMaterials;
    [SerializeField] private string[] skinNames = { "Black", "Blue", "Camo 1", "Camo 2", "Green", "Red", "White" };

    [Header("UI")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Text skinNameText;

    // ──────────────────────────────────────────────
    private int currentIndex;
    private RenderTexture renderTexture;

    // 초기화 시 한 번 캐시 — 이후 이름 비교 불필요
    private struct SkinRendererData
    {
        public SkinnedMeshRenderer renderer;
        public int[] skinSlots;
    }
    private List<SkinRendererData> skinRenderers = new List<SkinRendererData>();

    void Start()
    {
        // RenderTexture 런타임 생성
        renderTexture = new RenderTexture(
            (int)renderTextureSize.x, (int)renderTextureSize.y, 16, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        if (previewCamera != null) previewCamera.targetTexture = renderTexture;
        if (previewImage != null)  previewImage.texture = renderTexture;

        // sharedMaterials로 스킨 슬롯 인덱스 한 번만 캐시
        CacheSkinRenderers();

        prevButton?.onClick.AddListener(OnPrev);
        nextButton?.onClick.AddListener(OnNext);

        // Room CustomProperties에서 현재 스킨 불러오기
        currentIndex = GetRoomSkinIndex();
        ApplyPreview(currentIndex);
    }

    void Update()
    {
        if (previewRobotRoot != null)
            previewRobotRoot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    // ── Photon 콜백: 방 안의 누군가가 스킨을 바꾸면 이 화면도 즉시 갱신
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(PlayerSkinManager.SKIN_KEY))
        {
            currentIndex = GetRoomSkinIndex();
            ApplyPreview(currentIndex);
        }
    }

    void OnPrev()
    {
        int next = (currentIndex - 1 + skinMaterials.Length) % skinMaterials.Length;
        SaveToRoom(next);   // 저장 → OnRoomPropertiesUpdate 콜백으로 ApplyPreview 호출됨
    }

    void OnNext()
    {
        int next = (currentIndex + 1) % skinMaterials.Length;
        SaveToRoom(next);
    }

    void ApplyPreview(int index)
    {
        if (skinMaterials == null || index < 0 || index >= skinMaterials.Length) return;
        Material mat = skinMaterials[index];

        foreach (var data in skinRenderers)
        {
            if (data.renderer == null) continue;
            Material[] mats = data.renderer.sharedMaterials;
            foreach (int slot in data.skinSlots)
                if (slot < mats.Length) mats[slot] = mat;
            data.renderer.sharedMaterials = mats;
        }

        if (skinNameText != null && skinNames != null && index < skinNames.Length)
            skinNameText.text = skinNames[index];
    }

    // sharedMaterials 이름을 보고 스킨 슬롯 위치를 캐시 (Start에서 1회만 실행)
    void CacheSkinRenderers()
    {
        if (skinMaterials == null || previewRobotRoot == null) return;

        var skinMatNames = new HashSet<string>();
        foreach (var mat in skinMaterials)
            if (mat != null) skinMatNames.Add(mat.name);

        foreach (var r in previewRobotRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
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

    void SaveToRoom(int index)
    {
        if (!PhotonNetwork.InRoom) return;
        var props = new Hashtable { { PlayerSkinManager.SKIN_KEY, index } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    int GetRoomSkinIndex()
    {
        if (PhotonNetwork.InRoom &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                PlayerSkinManager.SKIN_KEY, out object val))
            return System.Convert.ToInt32(val);
        return 0;
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
