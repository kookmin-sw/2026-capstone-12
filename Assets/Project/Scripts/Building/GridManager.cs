using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 20;     // x 방향 칸 수
    public int height = 20;    // z 방향 칸 수
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;   // 그리드 시작점(왼쪽 아래 꼭짓점)

    [Header("Build Surface Sampling")]
    [SerializeField] private LayerMask buildSurfaceMask;
    [SerializeField] private float sampleStartHeight = 30f;
    [SerializeField] private float sampleRayDistance = 100f;
    [SerializeField] private float buildHeightOffset = 0.05f;
    [SerializeField] private bool bakeOnAwake = true;

    [Header("Debug")]
    [SerializeField] private bool drawValidCells = true;
    [SerializeField] private bool drawInvalidCells = false;
    [SerializeField] private bool drawOccupiedCells = true;
    [SerializeField] private bool drawBlockedCells = true;

    private bool[,] occupied;   // occupied[x, z] = true면 이미 누가 차지 중
    private bool[,] blocked;    // blocked[x, z] = true면 장애물로 설치 불가
    private bool[,] valid;
    private Vector3[,] surfacePoints;
    private Vector3[,] surfaceNormals;

    public LayerMask BuildSurfaceMask => buildSurfaceMask;

    private void Awake()
    {
        AllocateArrays();

        if (bakeOnAwake)
            BakeBuildableCells();
    }

    private void AllocateArrays()
    {
        occupied = new bool[width, height];
        blocked = new bool[width, height];
        valid = new bool[width, height];
        surfacePoints = new Vector3[width, height];
        surfaceNormals = new Vector3[width, height];
    }

    [ContextMenu("Bake Buildable Cells")]
    public void BakeBuildableCells()
    {
        if (occupied == null || occupied.GetLength(0) != width || occupied.GetLength(1) != height)
            AllocateArrays();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 flatCenter = GetFlatCellCenter(x, z);
                Vector3 rayStart = flatCenter + Vector3.up * sampleStartHeight;

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, sampleRayDistance, buildSurfaceMask, QueryTriggerInteraction.Ignore))
                {
                    valid[x, z] = true;
                    surfacePoints[x, z] = hit.point;
                    surfaceNormals[x, z] = hit.normal;
                }
                else
                {
                    valid[x, z] = false;
                    occupied[x, z] = false;
                    blocked[x, z] = false;
                    surfacePoints[x, z] = flatCenter;
                    surfaceNormals[x, z] = Vector3.up;
                }
            }
        }
    }

    public bool IsInside(int gx, int gz)
    {
        return gx >= 0 && gz >= 0 && gx < width && gz < height;
    }

    public bool IsValidCell(int gx, int gz)
    {
        return IsInside(gx, gz) && valid[gx, gz];
    }

    public bool IsOccupied(int gx, int gz)
    {
        return IsInside(gx, gz) && occupied[gx, gz];
    }

    // 장애물 차단 여부 확인
    public bool IsBlocked(int gx, int gz)
    {
        return IsInside(gx, gz) && blocked[gx, gz];
    }

    private Vector3 GetFlatCellCenter(int gx, int gz)
    {
        float x = origin.x + (gx + 0.5f) * cellSize;
        float z = origin.z + (gz + 0.5f) * cellSize;
        return new Vector3(x, origin.y, z);
    }

    // 월드 좌표 -> 그리드 좌표
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - origin;
        int gx = Mathf.FloorToInt(local.x / cellSize);
        int gz = Mathf.FloorToInt(local.z / cellSize);
        return new Vector2Int(gx, gz);
    }

    // 그리드 좌표 -> 셀 중심 월드 좌표
    public Vector3 GridToWorldCenter(int gx, int gz)
    {
        if (IsValidCell(gx, gz))
            return surfacePoints[gx, gz] + Vector3.up * buildHeightOffset;

        return GetFlatCellCenter(gx, gz);
    }

    public Vector2Int CenterToAnchor(Vector2Int centerCell, Vector2Int footprint, int rotationY)
    {
        Vector2Int size = GetRotatedFootprint(footprint, rotationY);
        int offsetX = size.x / 2; // 3->1, 2->1, 1->0
        int offsetZ = size.y / 2;
        return new Vector2Int(centerCell.x - offsetX, centerCell.y - offsetZ);
    }

    public Vector3 AnchorToWorldCenter(Vector2Int anchor, Vector2Int footprint, int rotationY)
    {
        if (TryGetPlacementPosition(anchor, footprint, rotationY, out Vector3 pos, out _))
            return pos;

        Vector2Int size = GetRotatedFootprint(footprint, rotationY);
        Vector3 baseCenter = GetFlatCellCenter(anchor.x, anchor.y);

        float offsetX = (size.x - 1) * 0.5f * cellSize;
        float offsetZ = (size.y - 1) * 0.5f * cellSize;

        return baseCenter + new Vector3(offsetX, 0f, offsetZ);
    }

    public bool TryGetPlacementPosition(Vector2Int anchor, Vector2Int footprint, int rotationY, out Vector3 worldPos, out Vector3 avgNormal)
    {
        worldPos = Vector3.zero;
        avgNormal = Vector3.up;

        Vector2Int size = GetRotatedFootprint(footprint, rotationY);

        Vector3 sumPos = Vector3.zero;
        Vector3 sumNormal = Vector3.zero;
        int count = 0;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int gx = anchor.x + x;
                int gz = anchor.y + z;

                if (!IsValidCell(gx, gz))
                    continue;

                sumPos += surfacePoints[gx, gz];
                sumNormal += surfaceNormals[gx, gz];
                count++;
            }
        }

        if (count == 0)
            return false;

        worldPos = (sumPos / count) + Vector3.up * buildHeightOffset;
        avgNormal = (sumNormal / count).normalized;

        if (avgNormal.sqrMagnitude < 0.0001f)
            avgNormal = Vector3.up;

        return true;
    }

    // 회전(0/90/180/270)에 따른 footprint 실제 크기 계산
    public Vector2Int GetRotatedFootprint(Vector2Int footprint, int rotationY)
    {
        int r = ((rotationY % 360) + 360) % 360;
        if (r == 90 || r == 270)
            return new Vector2Int(footprint.y, footprint.x);
        return footprint;
    }

    // 해당 영역이 비어있는지 확인
    public bool IsAreaFree(int baseGx, int baseGz, Vector2Int footprint, int rotationY)
    {
        Vector2Int size = GetRotatedFootprint(footprint, rotationY);

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int gx = baseGx + x;
                int gz = baseGz + z;

                if (!IsInside(gx, gz))
                    return false;

                if (!valid[gx, gz])
                    return false;

                if (occupied[gx, gz])
                    return false;

                // 설치 구조물 외 장애물 차단 검사
                if (blocked[gx, gz])
                    return false;
            }
        }
        return true;
    }

    // 점유 처리
    public void SetAreaOccupied(int baseGx, int baseGz, Vector2Int footprint, int rotationY, bool value)
    {
        Vector2Int size = GetRotatedFootprint(footprint, rotationY);

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int gx = baseGx + x;
                int gz = baseGz + z;

                if (!IsInside(gx, gz))
                    continue;

                occupied[gx, gz] = value;
            }
        }
    }

    // 잔해/소품 등 장애물 차단 처리
    public void SetAreaBlocked(int baseGx, int baseGz, Vector2Int footprint, int rotationY, bool value)
    {
        Vector2Int size = GetRotatedFootprint(footprint, rotationY);

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int gx = baseGx + x;
                int gz = baseGz + z;

                if (!IsInside(gx, gz))
                    continue;

                blocked[gx, gz] = value;
            }
        }
    }

    // 오브젝트 선택시, Scene뷰에 격자 그림
    private void OnDrawGizmosSelected()
    {
        if (width <= 0 || height <= 0)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos;

                if (Application.isPlaying && surfacePoints != null && x < surfacePoints.GetLength(0) && z < surfacePoints.GetLength(1))
                    pos = surfacePoints[x, z] + Vector3.up * 0.02f;
                else
                    pos = GetFlatCellCenter(x, z);

                if (Application.isPlaying && valid != null)
                {
                    if (occupied != null && occupied[x, z] && drawOccupiedCells)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0.05f, cellSize));
                    }
                    else if (blocked != null && blocked[x, z] && drawBlockedCells)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0.05f, cellSize));
                    }
                    else if (valid[x, z] && drawValidCells)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0.05f, cellSize));
                    }
                    else if (!valid[x, z] && drawInvalidCells)
                    {
                        Gizmos.color = Color.gray;
                        Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0.05f, cellSize));
                    }
                }
                else
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0.05f, cellSize));
                }
            }
        }
    }
}
