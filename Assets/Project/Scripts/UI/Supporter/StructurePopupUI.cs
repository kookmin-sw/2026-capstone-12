using UnityEngine;

public class StructurePopupUI : MonoBehaviour
{
    private  StructurePopupSpawner Spawner;
    public StructureSelectable Target { get; private set; }
    
    public void SetTarget(StructurePopupSpawner spawner, StructureSelectable target)
    {
        Spawner = spawner;
        Target = target;
        // 모든 자식 오브젝트의 Refresh 호출
        BroadcastMessage("Refresh", SendMessageOptions.DontRequireReceiver);
    }

    public void Close()
    {
        Spawner?.Close();
    }
}