using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 20;     // x 방향 칸 수
    public int height = 20;    // z 방향 칸 수
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;   // 그리드 시작점(왼쪽 아래 꼭짓점)

    // occupied[x, z] = true면 이미 누가 차지 중
    private bool[,] occupied;

    private void Awake()
    {
        occupied = new bool[width, height];
    }

    public bool IsInside(int gx, int gz)
    {
        return gx >= 0 && gz >= 0 && gx < width && gz < height;
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
        float x = origin.x + (gx + 0.5f) * cellSize;
        float z = origin.z + (gz + 0.5f) * cellSize;
        return new Vector3(x, origin.y, z);
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
        Vector2Int size = GetRotatedFootprint(footprint, rotationY);
        Vector3 baseCenter = GridToWorldCenter(anchor.x, anchor.y);

        float offsetX = (size.x - 1) * 0.5f * cellSize;
        float offsetZ = (size.y - 1) * 0.5f * cellSize;

        return baseCenter + new Vector3(offsetX, 0f, offsetZ);
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

                if (occupied[gx, gz])
                    return false;
            }
        }
        return true;
    }

    // 점유 처리
    public void OccupyArea(int baseGx, int baseGz, Vector2Int footprint, int rotationY, bool value)
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

    // 오브젝트 선택시, Scene뷰에 격자 그림
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;

        for (int x = 0; x <= width; x++)
        {
            Vector3 a = origin + new Vector3(x * cellSize, 0f, 0f); // 세로선 왼쪽
            Vector3 b = origin + new Vector3(x * cellSize, 0f, height * cellSize);  // 세로선 오른쪽
            Gizmos.DrawLine(a, b);
        }

        for (int z = 0; z <= height; z++)
        {
            Vector3 a = origin + new Vector3(0f, 0f, z * cellSize);
            Vector3 b = origin + new Vector3(width * cellSize, 0f, z * cellSize);
            Gizmos.DrawLine(a, b);
        }
    }
}
