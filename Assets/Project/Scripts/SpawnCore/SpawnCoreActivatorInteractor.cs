using Photon.Pun;
using UnityEngine;

public class SpawnCoreActivatorInteractor : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E; // 활성화 입력 키
    [SerializeField] private float interactDistance = 3f; // 상호작용 가능 거리
    [SerializeField] private LayerMask interactMask = ~0; // 상호작용 레이어

    private Camera playerCamera; // 크로스헤어 기준 카메라
    private PhotonView photonView; // 로컬 플레이어 판정용 뷰
    private SpawnCoreActivator highlightedActivator; // 현재 하이라이트 대상

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        if (!HasLocalAuthority())
            return;

        UpdateHighlight();

        if (!Input.GetKeyDown(interactKey))
            return;

        TryActivateFocusedCore(highlightedActivator);
    }

    private void OnDisable()
    {
        SetHighlightedActivator(null);
    }

    // 크로스헤어 대상 하이라이트 갱신
    private void UpdateHighlight()
    {
        if (highlightedActivator != null && !highlightedActivator.CanShowInteractionStatus)
        {
            SetHighlightedActivator(null);
            return;
        }

        SetHighlightedActivator(GetFocusedActivator());
    }

    // 크로스헤어 대상 액티베이터 조회
    private SpawnCoreActivator GetFocusedActivator()
    {
        if (playerCamera == null)
            return null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
            return null;

        SpawnCoreActivator activator = hit.collider.GetComponentInParent<SpawnCoreActivator>();
        if (activator == null || !activator.CanShowInteractionStatus)
            return null;

        return activator;
    }

    // 크로스헤어 대상 액티베이터 활성화 요청
    private void TryActivateFocusedCore(SpawnCoreActivator activator)
    {
        if (activator == null)
            return;

        int requesterViewId = photonView != null ? photonView.ViewID : 0;
        activator.RequestActivation(requesterViewId, transform.position);
    }

    // 하이라이트 대상 교체
    private void SetHighlightedActivator(SpawnCoreActivator activator)
    {
        if (highlightedActivator == activator)
            return;

        if (highlightedActivator != null)
            highlightedActivator.SetHighlighted(false);

        highlightedActivator = activator;

        if (highlightedActivator != null)
            highlightedActivator.SetHighlighted(true);
    }

    // 로컬 입력 처리 가능 여부
    private bool HasLocalAuthority()
    {
        if (!PhotonNetwork.IsConnected)
            return true;

        return photonView == null || photonView.IsMine;
    }
}
