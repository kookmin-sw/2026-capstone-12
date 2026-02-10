using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테스트용 데미지 버튼 (나중에 삭제 예정)
/// </summary>
public class TestDamageButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthManager healthManager;

    [Header("Test Settings")]
    [SerializeField] private float testDamage = 10f;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        healthManager.TakeDamage(testDamage);
    }
}