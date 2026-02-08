using UnityEngine;
using UnityEngine.UI;

public class SellButton : MonoBehaviour
{
    public float refundRate = 0.3f;
    private Button btn;
    private StructurePopupUI popup;

    private void Awake()
    {
        btn = GetComponent<Button>();
        popup = GetComponentInParent<StructurePopupUI>();
        btn.onClick.AddListener(OnClick);
    }

    public void Refresh()
    {
        btn.interactable = popup != null && popup.Target != null;
    }

    private void OnClick()
    {
        if (popup == null || popup.Target == null) return;        

        int refund = Mathf.RoundToInt(popup.Target.Cost * refundRate);
        if (ResourceManager.Instance != null) ResourceManager.Instance.AddGold(refund);

        popup.Target.Sell();
        Destroy(popup.gameObject);
    }
}