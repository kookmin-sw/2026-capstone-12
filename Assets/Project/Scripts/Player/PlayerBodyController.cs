using UnityEngine;

/// <summary>
/// Player 자식으로 넣은 Robot_Soldier의 달리기 애니메이션 제어
/// - 이동 중: Run_Aim 애니메이션 재생
/// - 정지 중: 애니메이션 멈춤
/// </summary>
public class PlayerBodyController : MonoBehaviour
{
    [Header("3인칭 바디")]
    [Tooltip("Player 자식으로 넣은 Robot_Soldier 오브젝트를 여기에 연결")]
    [SerializeField] private GameObject thirdPersonBody;

    private Animator bodyAnimator;
    private Vector3 lastPosition;

    void Start()
    {
        if (thirdPersonBody == null) return;
        bodyAnimator = thirdPersonBody.GetComponentInChildren<Animator>();
        lastPosition = transform.position;
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
