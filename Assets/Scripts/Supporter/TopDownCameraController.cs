using UnityEngine;

public class TopDownCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public LayerMask groundMask;

    [Header("Pan (RMB Drag)")]
    public float panSpeed = 1.0f;

    [Header("Zoom (Wheel to Cursor)")]
    public float zoomSpeed = 5f;
    public float minDistance = 5f;
    public float maxDistance = 60f;

    [Header("Rotate (LMB Drag)")]
    public float rotateSpeed = 0.2f;
    public float minPitch = 20f;
    public float maxPitch = 75f;

    [Header("Zoom Fallback Plane")]
    public bool fallbackToGroundPlane = true;
    public float groundPlaneY = 0f;

    private Vector3 lastMousePos;
    private bool isRotating;
    private float yaw;
    private float pitch;

    private void Reset()
    {
        cam = GetComponent<Camera>();
    }

    private void Awake()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = NormalizeAngle(e.x);
    }

    private void Update()
    {
        if (InputLock.IsLocked) return;
        
        if (cam == null)
            return;

        HandlePan();
        HandleZoom();
        HandleRotate();
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(1))
            lastMousePos = Input.mousePosition;

        if (!Input.GetMouseButton(1))
            return;

        Vector3 delta = Input.mousePosition - lastMousePos;
        lastMousePos = Input.mousePosition;

        if (delta.sqrMagnitude < 0.0001f)
            return;

        Vector3 right = cam.transform.right;
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;

        Vector3 move = (right * delta.x + forward * delta.y) * (panSpeed * 0.01f) * -1f;
        transform.position += move;
    }

    private void HandleZoom()
    {
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(wheel, 0f))
            return;

        ZoomToCursor(wheel);
    }

    private void HandleRotate()
    {
        if (Input.GetMouseButtonDown(2))
        {
            lastMousePos = Input.mousePosition;
            isRotating = true;
            return;
        }

        if (!Input.GetMouseButton(2) || !isRotating)
        {
            isRotating = false;
            return;
        }

        Vector3 delta = Input.mousePosition - lastMousePos;
        lastMousePos = Input.mousePosition;

        if (delta.sqrMagnitude < 0.01f)
            return;

        yaw += delta.x * rotateSpeed;
        pitch = Mathf.Clamp(pitch - delta.y * rotateSpeed, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ZoomToCursor(float wheel)
    {
        if (!TryGetMouseWorldOnGround(Input.mousePosition, out Vector3 hitPoint))
        {
            transform.position += cam.transform.forward * (wheel * zoomSpeed);
            return;
        }

        Vector3 camToHit = hitPoint - transform.position;
        float currentDist = camToHit.magnitude;
        if (currentDist < 0.001f)
            return;

        Vector3 dir = camToHit / currentDist;
        float targetDist = Mathf.Clamp(currentDist - (wheel * zoomSpeed), minDistance, maxDistance);
        float moveAmount = currentDist - targetDist;
        transform.position += dir * moveAmount;
    }

    private bool TryGetMouseWorldOnGround(Vector3 screenPos, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            worldPos = hit.point;
            return true;
        }

        if (!fallbackToGroundPlane)
            return false;

        float denom = Vector3.Dot(ray.direction, Vector3.up);
        if (Mathf.Abs(denom) < 0.0001f)
            return false;

        float t = (groundPlaneY - ray.origin.y) / denom;
        if (t < 0f)
            return false;

        worldPos = ray.origin + ray.direction * t;
        return true;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
