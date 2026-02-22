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
            Vector2Int anchor = gridManager.CenterToAnchor(centerCell, selectedType.footprint, rotationY);

            Vector3 snapped = gridManager.AnchorToWorldCenter(anchor, selectedType.footprint, rotationY);

            EnsureGhost();
            ghostObj.transform.position = snapped;
            ghostObj.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            bool hasMoney = ResourceManager.Instance == null || ResourceManager.Instance.CanAfford(selectedType.cost);
            bool canPlace = hasMoney && gridManager.IsAreaFree(anchor.x, anchor.y, selectedType.footprint, rotationY);
            ApplyGhostColor(canPlace);

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI())
                    return;

                if (canPlace)
                {
                    PlaceBuilding(anchor);
                }
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelBuildMode();
            }
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

    private void SelectType(int index)
    {
        if (index < 0 || index >= buildingTypes.Length)
            return;

        selectedType = buildingTypes[index];
        rotationY = 0;
        EnsureGhost();
    }

    private void HandleRotate()
    {
        if (selectedType == null || !selectedType.allowRotate)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
            rotationY = (rotationY + 270) % 360;
        if (Input.GetKeyDown(KeyCode.E))
            rotationY = (rotationY + 90) % 360;
    }

    private bool TryGetMouseWorldOnGround(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        if (topDownCamera == null)
            return false;

        Ray ray = topDownCamera.ScreenPointToRay(Input.mousePosition);

        // Ground 레이어에만 맞추고 싶으면 Physics.Raycast에 LayerMask 사용
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            worldPos = hit.point;
            return true;
        }

        return false;
    }

    private void PlaceBuilding(Vector2Int anchor)
    {
        if (selectedType == null)
            return;

        if (BuildNetManager.Instance == null)
            return;

        if (!Photon.Pun.PhotonNetwork.InRoom)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(selectedType.cost))
            return;

        // 로컬 생성/점유/차감은 하지 않고 "요청"만 보냄
        BuildNetManager.Instance.RequestPlace(selectedType.typeId, anchor.x, anchor.y, rotationY);
    }

    private void CancelBuildMode()
    {
        selectedType = null;
        rotationY = 0;
        DestroyGhost();
    }

    private void EnsureGhost()
    {
        if (ghostObj != null || selectedType == null || selectedType.prefab == null)
            return;

        ghostObj = Instantiate(selectedType.prefab);
        SetGhostMode(ghostObj);
    }

    private void DestroyGhost()
    {
        if (ghostObj != null)
            Destroy(ghostObj);
        ghostObj = null;
    }

    private void SetGhostMode(GameObject go)
    {
        if (go.GetComponent<GhostMarker>() == null)
            go.AddComponent<GhostMarker>();

        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

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

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    public void SelectBuilding(BuildingTypeSO type)
    {
        if (type == null)
            return;

        // 비용 부족이면 선택 자체를 차단
        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(type.cost))
            return;

        selectedType = type;
        rotationY = 0;
        EnsureGhost();
    }
}
