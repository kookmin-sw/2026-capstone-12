using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class SupportPlacementSystem : MonoBehaviour
{
    [SerializeField] private Camera topDownCamera; // Support 배치용 탑다운 카메라
    [SerializeField] private GridManager gridManager; // 지형 레이캐스트 레이어 조회용 Grid 참조
    [SerializeField] private Material ghostMaterial; // 배치 미리보기 표시용 고스트 재질
    [SerializeField] private float placementYOffset = 0.6f; // 지면과 아이템 사이 높이 보정값

    private SupportItemDefinition selectedItem; // 현재 배치 선택된 Support 아이템
    private GameObject ghostObj; // 마우스 위치 미리보기 오브젝트

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP 고스트 색상 변경용 셰이더 속성

    // 배치 참조 초기화
    private void Awake()
    {
        ResolveReferences();
    }

    // 그리드 비의존 Support 배치 입력 처리
    private void Update()
    {
        if (selectedItem == null)
        {
            DestroyGhost();
            return;
        }

        if (!TryGetMouseWorldOnGround(out Vector3 hitWorld))
        {
            DestroyGhost();
            return;
        }

        Vector3 placementPosition = hitWorld + Vector3.up * placementYOffset; // 지면 위 아이템 배치 위치
        EnsureGhost();

        if (ghostObj != null)
        {
            ghostObj.transform.position = placementPosition;
            ghostObj.transform.Rotate(Vector3.up, 30f * Time.deltaTime, Space.World);
        }

        ApplyGhostColor(CanPlaceSelected());

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI())
                return;

            if (CanPlaceSelected())
                PlaceSupportItem(placementPosition);
            else
                SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.ResourceLack);
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelPlacement();
    }

    // Support 슬롯 클릭 후 배치 대상 선택
    public void SelectSupportItem(SupportItemDefinition item)
    {
        if (item == null)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(item.cost))
        {
            SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.ResourceLack);
            return;
        }

        selectedItem = item;
        EnsureGhost();
    }

    // BuildSystem 기준 참조 재사용
    private void ResolveReferences()
    {
        BuildSystem buildSystem = FindObjectOfType<BuildSystem>(true);

        if (buildSystem != null)
        {
            if (topDownCamera == null)
                topDownCamera = buildSystem.topDownCamera;

            if (gridManager == null)
                gridManager = buildSystem.gridManager;

            if (ghostMaterial == null)
                ghostMaterial = buildSystem.ghostMaterial;
        }

        if (topDownCamera == null)
            topDownCamera = Camera.main;

        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>(true);
    }

    // 마우스 지형 위치 조회
    private bool TryGetMouseWorldOnGround(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        if (topDownCamera == null)
            ResolveReferences();

        if (topDownCamera == null)
            return false;

        Ray ray = topDownCamera.ScreenPointToRay(Input.mousePosition);
        int layerMask = gridManager != null ? gridManager.BuildSurfaceMask : Physics.DefaultRaycastLayers; // Build 지형 레이어 재사용

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, layerMask, QueryTriggerInteraction.Ignore))
        {
            worldPos = hit.point;
            return true;
        }

        return false;
    }

    // 현재 선택 아이템 배치 가능 여부 판정
    private bool CanPlaceSelected()
    {
        return selectedItem != null && (ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(selectedItem.cost));
    }

    // Support 아이템 월드 배치 요청
    private void PlaceSupportItem(Vector3 position)
    {
        if (selectedItem == null)
            return;

        int typeIndex = SupportItemCatalog.GetIndex(selectedItem.kind); // 네트워크 RPC용 아이템 인덱스
        if (typeIndex < 0)
            return;

        if (BuildNetManager.Instance != null && PhotonNetwork.InRoom)
        {
            BuildNetManager.Instance.RequestPlaceSupport(typeIndex, position);
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>(selectedItem.prefabResourcePath);
            if (prefab != null)
            {
                GameObject supportObject = Instantiate(prefab, position, Quaternion.identity); // 오프라인 테스트용 로컬 생성 오브젝트
                SupportPickupItem pickupItem = supportObject.GetComponent<SupportPickupItem>();
                if (pickupItem == null)
                    pickupItem = supportObject.AddComponent<SupportPickupItem>();

                pickupItem.Configure(-1, selectedItem.kind);
            }
        }

        CancelPlacement();
    }

    // Support 고스트 생성
    private void EnsureGhost()
    {
        if (ghostObj != null || selectedItem == null)
            return;

        GameObject prefab = Resources.Load<GameObject>(selectedItem.prefabResourcePath);
        if (prefab == null)
            return;

        ghostObj = Instantiate(prefab);
        SetGhostMode(ghostObj);
    }

    // Support 고스트 제거
    private void DestroyGhost()
    {
        if (ghostObj != null)
            Destroy(ghostObj);

        ghostObj = null;
    }

    // Support 배치 모드 종료
    private void CancelPlacement()
    {
        selectedItem = null;
        DestroyGhost();
    }

    // 아이템 고스트 상태 적용
    private void SetGhostMode(GameObject go)
    {
        if (go.GetComponent<GhostMarker>() == null)
            go.AddComponent<GhostMarker>();

        foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (SupportPickupItem pickupItem in go.GetComponentsInChildren<SupportPickupItem>(true))
            pickupItem.enabled = false;

        if (ghostMaterial == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
                materials[j] = ghostMaterial;

            renderers[i].materials = materials;
        }
    }

    // 배치 가능 색상 반영
    private void ApplyGhostColor(bool canPlace)
    {
        if (ghostObj == null)
            return;

        Color color = canPlace ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f); // 가능 여부 시각화 색상
        Renderer[] renderers = ghostObj.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            MaterialPropertyBlock block = new();
            renderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            renderers[i].SetPropertyBlock(block);
        }
    }

    // UI 클릭 배치 방지 판정
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }
}
