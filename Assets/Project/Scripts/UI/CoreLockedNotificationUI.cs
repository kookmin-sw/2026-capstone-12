using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class CoreLockedNotificationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text messageText;

    [Header("Message")]
    [SerializeField] private string lockedMessage = "아직 파괴할 수 없는 코어입니다!";

    [Header("Animation")]
    [SerializeField] private float fadeInDuration  = 0.2f;
    [SerializeField] private float holdDuration    = 1.8f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    private Coroutine showCoroutine;

    private void Start()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (messageText != null)
            messageText.text = lockedMessage;

        SpawnCoreManager mgr = SpawnCoreManager.Instance;
        if (mgr != null)
            mgr.OnLockedCoreAttackAttempted.AddListener(Show);
        else
            StartCoroutine(WaitAndSubscribe());
    }

    private void OnDestroy()
    {
        if (SpawnCoreManager.Instance != null)
            SpawnCoreManager.Instance.OnLockedCoreAttackAttempted.RemoveListener(Show);
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (SpawnCoreManager.Instance == null)
            yield return null;

        SpawnCoreManager.Instance.OnLockedCoreAttackAttempted.AddListener(Show);
    }

    private void Show()
    {
        if (!IsLocalShooter())
            return;

        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowRoutine());
    }

    private bool IsLocalShooter()
    {
        if (!PhotonNetwork.IsConnected)
            return true; // 오프라인은 슈터로 간주

        var player = PhotonNetwork.LocalPlayer;
        if (player == null)
            return true;

        return !player.CustomProperties.TryGetValue("Role", out object role) ||
               role as string != "Supporter";
    }

    private IEnumerator ShowRoutine()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdDuration);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        showCoroutine = null;
    }
}
