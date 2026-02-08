using UnityEngine;

public class StructurePopupUI : MonoBehaviour
{
    public StructureSelectable Target { get; private set; }

    public void SetTarget(StructureSelectable target)
    {
        Target = target;
        // 모든 자식 오브젝트의 Refresh 호출
        BroadcastMessage("Refresh", SendMessageOptions.DontRequireReceiver);
    }
}