using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public Camera topDownCamera;
    public GridManager gridManager;

    [Header("Building Types")]
    public BuildingTypeSO[] buildingTypes;

    [Header("Placement Visual")]
    public Material ghostMaterial;

    public event Action BuildModeEnded;

    private BuildingTypeSO selectedType;
    private GameObject ghostObj;
    private int rotationY; // 0, 90, 180, 270

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Update()
    {
        if (!Photon.Pun.PhotonNetwork.InRoom)
            return;

        HandleSelectType(); // UI의 구현으로 지우거나 연동할 필요가 있음

        if (selectedType == null)
        {
            DestroyGhost();
            return;
        }

        HandleRotate();

        if (TryGetMouseWorldOnGround(out Vector3 hitWorld))
        {
            Vector2Int centerCell = gridManager.WorldToGrid(hitWorld);

            if (!gridManager.IsInside(centerCell.x, centerCell.y))
            {
                DestroyGhost();
                return;
            }

            Vector2Int anchor = gridManager.CenterToAnchor(centerCell, selectedType.footprint, rotationY);

            EnsureGhost();

            bool hasPlacementPos = gridManager.TryGetPlacementPosition(anchor, selectedType.footprint, rotationY, out Vector3 snapped, out _);

            if (hasPlacementPos)
            {
                ghostObj.transform.position = snapped;
                ghostObj.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            }
            else
            {
                ghostObj.transform.position = hitWorld;
                ghostObj.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            }

            bool hasMoney = ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(selectedType.cost);
            bool canPlace = hasMoney && gridManager.IsAreaFree(anchor.x, anchor.y, selectedType.footprint, rotationY);
            ApplyGhostColor(canPlace);

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI())
                    return;

                if (canPlace)
                    PlaceBuilding(anchor);
                else if (!hasMoney)
                    SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.ResourceLack);
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                CancelBuildMode();
        }
        else
        {
            DestroyGhost();
        }
    }

    private void HandleSelectType()
    {
        // UI의 구현으로 지우거나 연동할 필요가 있음
        // 예시: 1,2,3 키로 건물 선택
        if (buildingTypes == null || buildingTypes.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectType(0);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectType(1);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SelectType(2);
    }

    // 인덱스로 구조물 타입을 선택하고 고스트를 준비
    private void SelectType(int index)
    {
        if (index < 0 || index >= buildingTypes.Length)
            return;

        selectedType = buildingTypes[index];
        rotationY = 0;
        EnsureGhost();
    }

    // 회전 가능한 구조물의 방향 입력을 처리
    private void HandleRotate()
    {
        if (selectedType == null || !selectedType.allowRotate)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
            rotationY = (rotationY + 270) % 360;
        if (Input.GetKeyDown(KeyCode.E))
            rotationY = (rotationY + 90) % 360;
    }

    // 마우스 위치를 설치 가능한 월드 좌표로 변환
    private bool TryGetMouseWorldOnGround(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        if (topDownCamera == null || gridManager == null)
            return false;

        Ray ray = topDownCamera.ScreenPointToRay(Input.mousePosition);

        // Ground 레이어에만 맞추고 싶으면 Physics.Raycast에 LayerMask 사용
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, gridManager.BuildSurfaceMask, QueryTriggerInteraction.Ignore))
        {
            worldPos = hit.point;
            return true;
        }

        return false;
    }

    // 네트워크 매니저에 설치 요청을 보내고 현재 선택은 유지
    private void PlaceBuilding(Vector2Int anchor)
    {
        if (selectedType == null)
            return;

        if (BuildNetManager.Instance == null)
            return;

        if (!Photon.Pun.PhotonNetwork.InRoom)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(selectedType.cost))
        {
            SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.ResourceLack);
            return;
        }

        // 로컬 생성/점유/차감은 하지 않고 "요청"만 보냄
        BuildNetManager.Instance.RequestPlace(selectedType.typeId, anchor.x, anchor.y, rotationY);
    }

    // 현재 빌드 모드를 종료하고 고스트를 정리
    private void CancelBuildMode()
    {
        selectedType = null;
        rotationY = 0;
        DestroyGhost();
        BuildModeEnded?.Invoke();
    }

    // 선택된 구조물의 고스트 오브젝트를 생성
    private void EnsureGhost()
    {
        if (ghostObj != null || selectedType == null || selectedType.prefab == null)
            return;

        ghostObj = Instantiate(selectedType.prefab);
        SetGhostMode(ghostObj);
    }

    // 현재 고스트 오브젝트를 제거
    private void DestroyGhost()
    {
        if (ghostObj != null)
            Destroy(ghostObj);
        ghostObj = null;
    }

    // 고스트 오브젝트를 배치 미리보기 상태로 변경
    private void SetGhostMode(GameObject go)
    {
        if (go.GetComponent<GhostMarker>() == null)
            go.AddComponent<GhostMarker>();

        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        foreach (var source in go.GetComponentsInChildren<PurificationLightSource>(true)) // 고스트 정화 등록 방지
            source.enabled = false;

        foreach (var pylonZone in go.GetComponentsInChildren<LightPylonPurificationZone>(true)) // 고스트 정화 설정 방지
            pylonZone.enabled = false;

        foreach (var light in go.GetComponentsInChildren<Light>(true)) // 고스트 조명 표시 방지
            light.enabled = false;

        // 고스트 재질 적용
        if (ghostMaterial != null)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                Material[] mats = rends[i].materials;
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = ghostMaterial;
                rends[i].materials = mats;
            }
        }
    }

    // 설치 가능 여부에 따라 고스트 색상을 갱신
    private void ApplyGhostColor(bool canPlace)
    {
        if (ghostObj == null)
            return;

        Color c = canPlace ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);

        Renderer[] rends = ghostObj.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rends.Length; i++)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            rends[i].GetPropertyBlock(mpb);

            // URP Lit 계열 대응
            mpb.SetColor(BaseColorId, c);

            rends[i].SetPropertyBlock(mpb);
        }
    }

    // 현재 마우스가 UI 위에 있는지 확인
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    // UI에서 선택한 구조물을 현재 빌드 대상으로 설정
    public void SelectBuilding(BuildingTypeSO type)
    {
        if (type == null)
            return;

        // 비용 부족이면 선택 자체를 차단
        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(type.cost))
        {
            SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.ResourceLack);
            return;
        }

        selectedType = type;
        rotationY = 0;
        EnsureGhost();
    }
}
