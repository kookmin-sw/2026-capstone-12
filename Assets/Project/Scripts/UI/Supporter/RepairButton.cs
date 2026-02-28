using UnityEngine;
using UnityEngine.UI;

public class RepairButton : MonoBehaviour
{
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

        btn.interactable = popup.Target.CanRepairLocal();
    }

    private void OnClick()
    {
        if (popup == null || popup.Target == null) return;
        popup.Target.RequestRepair();
        
        popup.Close();
    }
}