using UnityEngine;

/// <summary>
/// 프리팹에 부착하면 Start 시점에 MinimapIcon을 자동 추가
/// - 적 프리팹, 플레이어 프리팹 등에 부착
/// </summary>
public class MinimapIconAutoAttach : MonoBehaviour
{
    [SerializeField] private MinimapIcon.IconType iconType = MinimapIcon.IconType.Enemy;
    [SerializeField] private Color iconColor = Color.red;
    [SerializeField] private float iconSize = 3f;

    private void Start()
    {
        MinimapIcon icon = gameObject.AddComponent<MinimapIcon>();
        // SerializeField는 런타임 AddComponent로 설정 불가 → public 초기화 메서드 사용
        icon.Init(iconType, iconColor, iconSize);
    }
}
