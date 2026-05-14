using Photon.Pun;
using UnityEngine;

/// <summary>
/// FPS 플레이어 컨트롤러
/// - WASD 이동
/// - 마우스 시점 회전
/// - CharacterController 기반
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    private float bonusSpeed = 0f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private LayerMask groundMask = ~0; // 인스펙터에서 Ground 레이어만 지정 권장

    [Header("Footstep Sound")]
    [SerializeField] private float walkStepInterval = 0.45f; // 걷기 발소리 재생 간격
    [SerializeField] private float runStepInterval = 0.28f; // 달리기 발소리 재생 간격

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    // Components
    private CharacterController controller;
    private Transform cameraTransform;
    private PhotonView photonView; // 로컬 Shooter 판정용 PhotonView

    // Look
    private float verticalRotation = 0f;

    // Movement
    private Vector3 velocity;
    private bool isGrounded;
    private bool movementLocked; // 사망 중 제자리 시점 회전만 허용하기 위한 이동 잠금

    // 사망 중 이동 차단, 마우스 룩만 허용
    private bool lookOnlyMode = false;

    private bool wasSettingsOpen = false;
    private float nextFootstepTime; // 다음 발소리 재생 가능 시각

    void Start()
    {
        // 컴포넌트 참조 가져오기
        controller = GetComponent<CharacterController>();
        photonView = GetComponent<PhotonView>();
        // Camera.main 대신 자식 카메라를 직접 참조 (Supporter Camera와 태그 충돌 방지)
        cameraTransform = GetComponentInChildren<Camera>().transform;
    }

	void OnEnable()
	{
		// 마우스 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
	}

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

	void Update()
    {
        bool settingsOpen = settingsPanel != null && settingsPanel.activeSelf;

        // ESC: 설정창 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null)
            {
                if (!settingsOpen)
                {
                    settingsPanel.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    InputLock.Lock();
                }
                else
                {
                    settingsPanel.SetActive(false);
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // 설정창이 닫힌 순간 커서 다시 잠금 + 입력 잠금 해제
        if (wasSettingsOpen && !settingsOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            InputLock.Unlock();
        }
        wasSettingsOpen = settingsOpen;

        if (!movementLocked && !settingsOpen)
        {
            HandleMovement();
        }

        if (!settingsOpen)
        {
            HandleLook();
        }

        // 설정창 닫혀있을 때만 클릭으로 커서 재잠금
        if (!settingsOpen && Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// WASD 이동 및 중력 처리
    /// </summary>
    void HandleMovement()
    {
        if (lookOnlyMode)
        {
            // 사망 중: 이동/점프 입력 차단, 중력만 적용
            if (controller.isGrounded && velocity.y < 0) velocity.y = -0.5f;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            return;
        }

        // 입력 받기
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S

        // 달리기 체크 (Left Shift)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = (isSprinting ? sprintSpeed : moveSpeed) + bonusSpeed;

        // 이동 방향 계산 (플레이어 기준 로컬 좌표)
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        // 이동 적용
        controller.Move(move * currentSpeed * Time.deltaTime);

        // 바닥 체크 (CharacterController 실제 하단 위치 기준 Raycast)
        Vector3 bottomCenter = transform.position + controller.center
                               + Vector3.down * (controller.height * 0.5f);
        isGrounded = controller.isGrounded || Physics.Raycast(
            bottomCenter + Vector3.up * controller.skinWidth,
            Vector3.down,
            groundCheckDistance + controller.skinWidth,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        // 중력 적용
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -0.5f;
        }

        TryPlayFootstep(horizontal, vertical, isSprinting);

        // 점프 (Space)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// 마우스 시점 회전
    /// </summary>
    void HandleLook()
    {
        // SettingsManager에서 감도 가져오기
        float sensitivity = mouseSensitivity;
        if (SettingsManager.Instance != null)
        {
            sensitivity = SettingsManager.Instance.mouseSensitivity;
        }

        // 마우스 입력
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // 좌우 회전 (Y축 - 플레이어 전체)
        transform.Rotate(Vector3.up * mouseX);

        // 상하 회전 (X축 - 카메라만)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    /// <summary>
    /// 컨트롤러 활성화/비활성화 (사망용)
    /// </summary>
    public void SetBonusSpeed(float bonus)
    {
        bonusSpeed = bonus;
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    // 사망 시 이동/점프 차단, 마우스 룩은 유지 (커서 잠금 상태 그대로)
    public void SetLookOnly(bool value)
    {
        lookOnlyMode = value;
    }

    /// <summary>
    /// 이동 상태에 따른 발소리 재생 요청
    /// </summary>
    private void TryPlayFootstep(float horizontal, float vertical, bool isSprinting)
    {
        if (!CanRequestFootstepSound() || !isGrounded || Time.time < nextFootstepTime)
            return;

        Vector2 moveInput = new Vector2(horizontal, vertical);
        if (moveInput.sqrMagnitude < 0.01f)
            return;

        GameSoundType soundType = isSprinting ? GameSoundType.ShooterRun : GameSoundType.ShooterWalk;
        float interval = isSprinting ? runStepInterval : walkStepInterval;
        nextFootstepTime = Time.time + Mathf.Max(0.05f, interval);
        SoundNet.Instance.RequestPlayAt(soundType, transform.position);
    }

    /// <summary>
    /// 로컬 Shooter 발소리 요청 가능 여부
    /// </summary>
    private bool CanRequestFootstepSound()
    {
        if (SoundNet.Instance == null)
            return false;

        return !PhotonNetwork.InRoom || photonView == null || photonView.IsMine;
    }
}
