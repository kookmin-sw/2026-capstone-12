using UnityEngine;
using UnityEngine.UI;

public class RepairButton : MonoBehaviour
{
    public float repairRate = 0.1f;
    public int repairCost = 0;

    private Button btn;
    private StructurePopupUI popup;

    private void Awake()
    {
        btn = GetComponent<Button>();
        popup = GetComponentInParent<StructurePopupUI>();
        btn.onClick.AddListener(OnClick);
    }

	private void Start()
	{
		repairCost = (int)(popup.Target.Cost * repairRate);
	}

	public void Refresh()
    {
        if (popup == null || popup.Target == null)
        {
            btn.interactable = false;
            return;
        }

        if (popup.Target.IsFullHp)
        {
            btn.interactable = false;
            return;
        }

        btn.interactable = popup.Target.CanRepair(repairCost);
    }

    private void OnClick()
    {
        if (popup == null || popup.Target == null) return;
        if (popup.Target.IsFullHp) return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.TrySpend(repairCost))
            return;

        popup.Target.Repair();
        Destroy(popup.gameObject);
    }
}