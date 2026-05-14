using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class StructureHealthBar : MonoBehaviour
{
    [SerializeField] private float visibleMaxDistance = 120f; // 체력바 표시 최대 거리
    [SerializeField] private Canvas healthCanvas; // 프리팹 체력바 캔버스
    [SerializeField] private Image fillImage; // 프리팹 체력 채움 이미지
    private readonly Color fullColor = new Color(0f, 0.78f, 0f); // 양호 체력 색상
    private readonly Color midColor = new Color(1f, 0.84f, 0f); // 주의 체력 색상
    private readonly Color lowColor = new Color(1f, 0.16f, 0.12f); // 위험 체력 색상

    private BuildingHealthNet health; // 체력 동기화 대상
    private RectTransform fillRect; // 체력 채움 영역
    private Camera supporterCamera; // Supporter 기준 카메라
    private float fillWidth; // 체력바 기준 너비
    private float fillHeight; // 체력바 기준 높이
    private float lastCurrentHp; // 마지막 현재 체력
    private float lastMaxHp; // 마지막 최대 체력
    private bool subscribed; // 체력 이벤트 구독 상태

    private void Awake()
    {
        if (health == null)
            health = GetComponent<BuildingHealthNet>();

        ResolvePrefabReferences();
        SetVisible(false);
    }

    private void OnEnable()
    {
        ResolvePrefabReferences();
        if (!subscribed && health != null)
        {
            health.OnHpChanged += HandleHpChanged;
            subscribed = true;
            Refresh(health.CurrentHp, health.MaxHp);
        }
    }

    private void LateUpdate()
    {
        if (healthCanvas == null)
            return;

        Refresh(lastCurrentHp, lastMaxHp);
        if (!healthCanvas.gameObject.activeSelf)
            return;

        ResolveSupporterCamera();
        if (supporterCamera != null)
            healthCanvas.transform.rotation = supporterCamera.transform.rotation;
    }

    private void OnDisable()
    {
        UnsubscribeHealth();
    }

    /// <summary>
    /// 체력 동기화 대상 설정
    /// </summary>
    public void Configure(BuildingHealthNet targetHealth)
    {
        ResolvePrefabReferences();
        UnsubscribeHealth();
        health = targetHealth;

        if (isActiveAndEnabled && health != null)
        {
            health.OnHpChanged += HandleHpChanged;
            subscribed = true;
            Refresh(health.CurrentHp, health.MaxHp);
        }
    }

    /// <summary>
    /// 프리팹 참조 자동 연결
    /// </summary>
    private void ResolvePrefabReferences()
    {
        if (healthCanvas == null)
            healthCanvas = GetComponentInChildren<Canvas>(true);

        if (fillImage == null)
        {
            Transform fill = healthCanvas != null ? healthCanvas.transform.Find("Fill") : transform.Find("SupporterStructureHealthBar/Fill");
            if (fill != null)
                fillImage = fill.GetComponent<Image>();
        }

        if (fillImage == null)
            return;

        fillRect = fillImage.rectTransform;
        fillWidth = fillRect.sizeDelta.x;
        fillHeight = fillRect.sizeDelta.y;
    }

    /// <summary>
    /// 체력 변경에 따른 표시 갱신
    /// </summary>
    private void HandleHpChanged(BuildingHealthNet buildingHealth, float currentHp, float maxHp)
    {
        Refresh(currentHp, maxHp);
    }

    /// <summary>
    /// 체력 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeHealth()
    {
        if (subscribed && health != null)
            health.OnHpChanged -= HandleHpChanged;

        subscribed = false;
    }

    /// <summary>
    /// 체력바 표시 상태와 값 갱신
    /// </summary>
    private void Refresh(float currentHp, float maxHp)
    {
        lastCurrentHp = currentHp;
        lastMaxHp = maxHp;

        bool canShow = IsLocalSupporter() && maxHp > 0f && currentHp > 0f && currentHp < maxHp;
        if (healthCanvas == null || fillRect == null || fillImage == null)
            return;

        ResolveSupporterCamera();
        if (canShow && supporterCamera != null)
        {
            float distance = Vector3.Distance(supporterCamera.transform.position, transform.position);
            canShow = distance <= visibleMaxDistance;
        }

        SetVisible(canShow);
        if (!canShow)
            return;

        float ratio = Mathf.Clamp01(currentHp / maxHp);
        fillRect.sizeDelta = new Vector2(fillWidth * ratio, fillHeight);
        fillImage.color = GetHealthColor(ratio);
    }

    /// <summary>
    /// 체력바 활성 상태 반영
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (healthCanvas != null && healthCanvas.gameObject.activeSelf != visible)
            healthCanvas.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 체력 비율 색상 조회
    /// </summary>
    private Color GetHealthColor(float ratio)
    {
        if (ratio > 0.5f)
            return fullColor;

        if (ratio > 0.25f)
            return midColor;

        return lowColor;
    }

    /// <summary>
    /// Supporter 카메라 참조 보정
    /// </summary>
    private void ResolveSupporterCamera()
    {
        if (supporterCamera != null && supporterCamera.isActiveAndEnabled)
            return;

        TopDownCameraController[] controllers = FindObjectsOfType<TopDownCameraController>();
        for (int i = 0; i < controllers.Length; i++)
        {
            Camera cam = controllers[i].Cam != null ? controllers[i].Cam : controllers[i].GetComponent<Camera>();
            if (cam != null && cam.isActiveAndEnabled)
            {
                supporterCamera = cam;
                return;
            }
        }
    }

    /// <summary>
    /// 로컬 Supporter 역할 여부
    /// </summary>
    private bool IsLocalSupporter()
    {
        if (!PhotonNetwork.IsConnected)
            return true;

        if (PhotonNetwork.LocalPlayer == null)
            return false;

        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object roleValue))
            return false;

        return roleValue as string == "Supporter";
    }
}
