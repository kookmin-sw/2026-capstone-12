using Photon.Pun;
using UnityEngine;

/// <summary>
/// Player 자식으로 넣은 Robot_Soldier의 달리기 애니메이션 제어
/// - 이동 중: Run_Aim 애니메이션 재생
/// - 정지 중: 애니메이션 멈춤
/// - 역할에 따라 로봇 몸체 / FPS_Arms 가시성 제어
///   · 로컬 슈터  → 로봇 몸체 숨김, FPS_Arms 표시
///   · 로컬 서포터 → 로봇 몸체 표시, FPS_Arms 숨김
/// </summary>
public class PlayerBodyController : MonoBehaviourPun
{
    [Header("3인칭 바디")]
    [Tooltip("Player 자식으로 넣은 Robot_Soldier 오브젝트를 여기에 연결")]
    [SerializeField] private GameObject thirdPersonBody;

    private Animator bodyAnimator;
    private Vector3 lastPosition;
    private Transform fpsArmsTransform;

    void Start()
    {
        if (thirdPersonBody == null) return;
        bodyAnimator = thirdPersonBody.GetComponentInChildren<Animator>();
        lastPosition = transform.position;

        // Camera.main 대신 자식 카메라 직접 참조 (Supporter Camera 태그 충돌 방지)
        Camera mainCam = GetComponentInChildren<Camera>();
        if (mainCam != null)
            fpsArmsTransform = mainCam.transform.Find("FPS_Arms");

        ApplyRoleVisibility();
    }

    /// <summary>
    /// CustomProperties["Role"] 기준으로 가시성 설정
    /// photonView.IsMine 대신 Role을 사용해 타이밍 이슈 방지
    /// </summary>
    void ApplyRoleVisibility()
    {
        bool isLocalShooter;
        if (!PhotonNetwork.IsConnected)
        {
            isLocalShooter = true; // 단독 에디터 테스트는 슈터로 처리
        }
        else
        {
            var props = PhotonNetwork.LocalPlayer.CustomProperties;
            isLocalShooter = props.ContainsKey("Role") && (string)props["Role"] == "Shooter";
        }

        // 로봇 몸체: 슈터 본인에게는 숨김 (FPS_Arms가 대체), 서포터에게는 보임
        foreach (var r in thirdPersonBody.GetComponentsInChildren<SkinnedMeshRenderer>())
            r.enabled = !isLocalShooter;

        if (fpsArmsTransform != null)
            fpsArmsTransform.gameObject.SetActive(isLocalShooter);
    }

    // 사망 시 FPS_Arms를 숨기고 부활 시 역할 기준으로 복원
    public void SetArmsVisible(bool visible)
    {
        if (fpsArmsTransform != null)
            fpsArmsTransform.gameObject.SetActive(visible);
    }

    void Update()
    {
        if (bodyAnimator == null) return;

        float moved = (transform.position - lastPosition).magnitude;
        lastPosition = transform.position;

        float targetSpeed = moved > 0.001f ? 1f : 0f;

        // 속도를 서서히 올리고 내려서 자연스럽게 전환
        bodyAnimator.speed = Mathf.Lerp(bodyAnimator.speed, targetSpeed, Time.deltaTime * 8f);
    }
}
