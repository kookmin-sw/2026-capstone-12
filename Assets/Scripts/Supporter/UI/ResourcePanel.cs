using TMPro;
using UnityEngine;

public class ResourcePanel : MonoBehaviour
{
    public TextMeshProUGUI goldText;

    private bool subscribed;

    private void Update()
    {
        if (subscribed) return;
        if (ResourceManager.Instance == null) return;

        ResourceManager.Instance.OnGoldChanged += HandleGoldChanged;
        subscribed = true;

        HandleGoldChanged(ResourceManager.Instance.Gold);
    }

    private void OnDisable()
    {
        if (subscribed && ResourceManager.Instance != null)
            ResourceManager.Instance.OnGoldChanged -= HandleGoldChanged;

        subscribed = false;
    }

    private void HandleGoldChanged(int value)
    {
        if (goldText != null)
            goldText.text = $"Gold: {value}";
    }
}
