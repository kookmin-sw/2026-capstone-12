using UnityEngine;

public class SpawnCoreActivator : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private bool triggerOnEnable = false; // 활성화 시 스폰 시작 여부
    [SerializeField] private SpawnCore targetCore; // 활성화 대상 코어
    [SerializeField] private float activationDistance = 3f; // 활성화 가능 거리
    [SerializeField] private Color highlightColor = new Color(0.2f, 1f, 0.45f, 1f); // 상호작용 가능 색상
    [SerializeField] private Color blockedColor = new Color(1f, 0.12f, 0.08f, 1f); // 활성화 불가 색상

    private Renderer[] renderers; // 하이라이트 적용 렌더러
    private MaterialPropertyBlock propertyBlock; // 하이라이트 속성 블록
    private bool highlighted; // 하이라이트 적용 여부
    private bool highlightedAsBlocked; // 활성화 불가 표시 여부

    public bool CanShowInteractionStatus => targetCore != null && !targetCore.IsSpawnActivated;
    public bool CanRequestActivation => CanShowInteractionStatus && (SpawnCoreManager.Instance == null || SpawnCoreManager.Instance.CanActivateCore(targetCore));

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (!triggerOnEnable)
            return;

        RequestActivation(0, transform.position);
    }

    // 플레이어 입력 기반 활성화 요청
    public void RequestActivation(int requesterViewId, Vector3 fallbackRequesterPosition)
    {
        if (targetCore == null)
        {
            Debug.LogWarning($"{nameof(SpawnCoreActivator)}: Target SpawnCore is not assigned.", this);
            return;
        }

        SpawnCoreManager.Instance?.RequestActivateCore(targetCore, transform.position, activationDistance, requesterViewId, fallbackRequesterPosition);
    }

    // 상호작용 가능 표시 적용
    public void SetHighlighted(bool active)
    {
        active = active && CanShowInteractionStatus;
        bool blocked = active && !CanRequestActivation;

        if (highlighted == active && highlightedAsBlocked == blocked)
            return;

        highlighted = active;
        highlightedAsBlocked = blocked;
        Color targetColor = highlightedAsBlocked ? blockedColor : highlightColor;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null)
                continue;

            if (!highlighted)
            {
                targetRenderer.SetPropertyBlock(null);
                continue;
            }

            propertyBlock.Clear();
            propertyBlock.SetColor(BaseColorId, targetColor);
            propertyBlock.SetColor(ColorId, targetColor);
            propertyBlock.SetColor(EmissionColorId, targetColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
