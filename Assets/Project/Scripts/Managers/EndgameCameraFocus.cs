using System.Collections;
using UnityEngine;

public class EndgameCameraFocus : MonoBehaviour
{
    private static EndgameCameraFocus _instance;

    public static EndgameCameraFocus Get()
    {
        if (_instance != null)
            return _instance;

        var go = new GameObject("EndgameCameraFocus");
        _instance = go.AddComponent<EndgameCameraFocus>();
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void FocusOn(Vector3 targetWorldPos, float duration)
    {
        if (targetWorldPos == Vector3.zero)
            return;

        StartCoroutine(DoFocus(targetWorldPos, duration));
    }

    private IEnumerator DoFocus(Vector3 targetPos, float duration)
    {
        float elapsed = 0f;

        // 서포터: 탑다운 카메라를 폭발 위치가 화면 중앙에 오도록 패닝
        TopDownCameraController topDown = FindObjectOfType<TopDownCameraController>();
        if (topDown != null)
        {
            Vector3 startPos = topDown.transform.position;
            if (!topDown.TryGetCenteredPositionFor(targetPos, out Vector3 destPos))
                destPos = new Vector3(targetPos.x, startPos.y, targetPos.z);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                topDown.transform.position = Vector3.Lerp(startPos, destPos, t);
                yield return null;
            }
            yield break;
        }

        // 슈터: FPS 카메라를 폭발 방향으로 회전
        PlayerController shooter = FindObjectOfType<PlayerController>();
        if (shooter != null)
        {
            Camera cam = shooter.GetComponentInChildren<Camera>();
            if (cam == null)
                yield break;

            Quaternion startBodyRot = shooter.transform.rotation;
            Quaternion startCamRot = cam.transform.localRotation;

            Vector3 toTarget = targetPos - cam.transform.position;
            float yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(Mathf.Clamp(toTarget.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            Quaternion destBodyRot = Quaternion.Euler(0f, yaw, 0f);
            Quaternion destCamRot = Quaternion.Euler(pitch, 0f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                shooter.transform.rotation = Quaternion.Slerp(startBodyRot, destBodyRot, t);
                cam.transform.localRotation = Quaternion.Slerp(startCamRot, destCamRot, t);
                yield return null;
            }
        }
    }
}
