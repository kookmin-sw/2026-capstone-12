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
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;

    // Components
    private CharacterController controller;
    private Transform cameraTransform;

    // Look
    private float verticalRotation = 0f;

    // Movement
    private Vector3 velocity;

    void Start()
    {
        // 컴포넌트 참조 가져오기
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
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
        HandleMovement();
        HandleLook();

        // ESC로 커서 해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 화면 클릭 시 커서 다시 잠금
        if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
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
        // 입력 받기
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S

        // 달리기 체크 (Left Shift)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // 이동 방향 계산 (플레이어 기준 로컬 좌표)
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        // 이동 적용
        controller.Move(move * currentSpeed * Time.deltaTime);

        // 중력 적용
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 바닥에 붙어있기
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
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }
}