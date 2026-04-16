using UnityEngine;

[DisallowMultipleComponent]
public class EnemyNestGroup : MonoBehaviour
{
    [SerializeField] private SpawnCore ownerCore; // 하위 Nest가 공유하는 소속 Core 참조

    public SpawnCore OwnerCore => ownerCore; // 하위 Nest의 자동 소속 연결용 속성
    public bool IsCoreAlive => ownerCore == null || !ownerCore.IsDestroyed; // Group 스폰 가능 Core 생존 상태
}
