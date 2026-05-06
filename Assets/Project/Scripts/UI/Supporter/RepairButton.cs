using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RepairButton : MonoBehaviour, IPointerClickHandler
{
    private Button btn;
    private StructurePopupUI popup;

    private void Awake()
    {
        btn = GetComponent<Button>();
        popup = GetComponentInParent<StructurePopupUI>();
        SupporterUISoundEmitter soundEmitter = GetComponent<SupporterUISoundEmitter>();
        if (soundEmitter == null)
            soundEmitter = gameObject.AddComponent<SupporterUISoundEmitter>();

        soundEmitter.ConfigureClickSound(SupporterUISoundType.Repair);

        btn.onClick.AddListener(OnClick);
    }

	public void Refresh()
    {
        bool canShow = popup != null && popup.Target != null && popup.Target.CanRepair;
        gameObject.SetActive(canShow);

        if (!canShow)
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
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (btn != null && btn.interactable)
            return;

        if (popup != null && popup.Target != null && popup.Target.CanRepair && !popup.Target.IsFullHp && !popup.Target.CanRepairLocal())
            SupporterUISoundPlayer.Instance?.Play(SupporterUISoundType.ResourceLack);
    }
}
