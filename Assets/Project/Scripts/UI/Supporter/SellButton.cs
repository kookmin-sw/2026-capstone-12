using UnityEngine;
using UnityEngine.UI;


public class SellButton : MonoBehaviour
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
        btn.interactable = popup != null && popup.Target != null;
    }

    private void OnClick()
    {
        if (popup == null || popup.Target == null) return;
        popup.Target.RequestSell();
        
        popup.Close();
    }
}