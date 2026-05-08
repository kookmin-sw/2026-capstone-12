using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildNetManager : MonoBehaviourPun
{
    public static event Action<SupporterItemSO> SupportSpawned;

    public static BuildNetManager Instance { get; private set; }
    public GridManager grid;
    public BuildSystem buildSystem;

    public BuildingTypeSO[] types;
    public SupporterItemSO[] supportItemTypes;

    private readonly Dictionary<int, SupportPickupItem> supportItems = new();
    private readonly Dictionary<int, float> nextBeaconPlaceTimes = new();
    private readonly List<float> activeBeaconExpireTimes = new();
    private int nextSupportId = 1;

    private void Awake()
    {
        Instance = this;
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (buildSystem == null) buildSystem = FindObjectOfType<BuildSystem>();
    }

    public void RequestPlace(int typeId, int anchorX, int anchorZ, int rotY)
    {
        photonView.RPC(nameof(RpcRequestPlace), RpcTarget.MasterClient, typeId, anchorX, anchorZ, rotY, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RpcRequestPlace(int typeId, int anchorX, int anchorZ, int rotY, int requesterActor)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        BuildingTypeSO type = types[typeId];
        if (type == null) return;

        if (!grid.IsAreaFree(anchorX, anchorZ, type.footprint, rotY))
            return;

        if (ResourceNet.Instance != null && !ResourceNet.Instance.MasterTrySpendMoney(type.cost))
            return;

        Vector3 pos = grid.AnchorToWorldCenter(new Vector2Int(anchorX, anchorZ), type.footprint, rotY);
        Quaternion rot = Quaternion.Euler(0f, rotY, 0f);
        object[] instData = { typeId, anchorX, anchorZ, rotY };
        PhotonNetwork.Instantiate(type.photonPrefabPath, pos, rot, 0, instData);
        SoundNet.Instance?.RequestPlayAt(GameSoundType.BuildStructure, pos);
    }

    public void RequestPlaceSupport(int supportTypeIndex, Vector3 position)
    {
        int requesterActor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
        photonView.RPC(nameof(RpcRequestPlaceSupport), RpcTarget.MasterClient, supportTypeIndex, position, requesterActor);
    }

    [PunRPC]
    private void RpcRequestPlaceSupport(int supportTypeIndex, Vector3 position, int requesterActor)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        SupporterItemSO item = GetSupportItem(supportTypeIndex);
        if (item == null) return;

        if (item.kind == SupportItemKind.PurificationBeacon && !CanPlacePurificationBeacon(item, position, requesterActor))
            return;

        if (ResourceNet.Instance != null && !ResourceNet.Instance.MasterTrySpendMoney(item.cost))
            return;

        if (item.kind == SupportItemKind.PurificationBeacon)
        {
            RegisterPurificationBeaconPlacement(item);
            photonView.RPC(nameof(RpcSpawnSupport), RpcTarget.All, supportTypeIndex, position, -1);
            SoundNet.Instance?.RequestPlayAt(GameSoundType.SupplyItem, position);
            return;
        }

        int supportId = nextSupportId++;
        photonView.RPC(nameof(RpcSpawnSupport), RpcTarget.All, supportTypeIndex, position, supportId);
        SoundNet.Instance?.RequestPlayAt(GameSoundType.SupplyItem, position);
    }

    public void RequestConsumeSupport(int supportId)
    {
        photonView.RPC(nameof(RpcRequestConsumeSupport), RpcTarget.MasterClient, supportId);
    }

    [PunRPC]
    private void RpcRequestConsumeSupport(int supportId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC(nameof(RpcConsumeSupport), RpcTarget.All, supportId);
    }

    [PunRPC]
    private void RpcSpawnSupport(int supportTypeIndex, Vector3 position, int supportId)
    {
        SupporterItemSO item = GetSupportItem(supportTypeIndex);
        if (item == null) return;

        GameObject prefab = item.prefab != null ? item.prefab : Resources.Load<GameObject>(item.prefabResourcePath);
        if (prefab == null) return;

        PurificationBeaconSupportEffectSO beaconEffect = item.kind == SupportItemKind.PurificationBeacon
            ? GetPurificationBeaconEffect(item)
            : null;
        if (item.kind == SupportItemKind.PurificationBeacon && beaconEffect == null)
            return;

        GameObject supportObject = Instantiate(prefab, position, Quaternion.identity);
        if (item.kind == SupportItemKind.PurificationBeacon)
        {
            foreach (SupportPickupItem existingPickup in supportObject.GetComponentsInChildren<SupportPickupItem>(true))
                Destroy(existingPickup);

            PurificationBeaconNet beacon = supportObject.GetComponent<PurificationBeaconNet>();
            if (beacon == null)
                beacon = supportObject.AddComponent<PurificationBeaconNet>();

            beacon.Configure(beaconEffect.radius, beaconEffect.activeDuration);
            SupportSpawned?.Invoke(item);
            return;
        }

        SupportPickupItem pickupItem = supportObject.GetComponent<SupportPickupItem>();
        if (pickupItem == null)
            pickupItem = supportObject.AddComponent<SupportPickupItem>();

        pickupItem.Configure(supportId, item);
        supportItems[supportId] = pickupItem;
        SupportSpawned?.Invoke(item);
    }

    [PunRPC]
    private void RpcConsumeSupport(int supportId)
    {
        if (!supportItems.TryGetValue(supportId, out SupportPickupItem pickupItem) || pickupItem == null)
        {
            SupportPickupItem[] allItems = FindObjectsOfType<SupportPickupItem>();
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i].SupportId == supportId)
                {
                    pickupItem = allItems[i];
                    break;
                }
            }
        }

        if (pickupItem == null)
            return;

        supportItems.Remove(supportId);
        pickupItem.StartConsuming();
    }

    public void RequestSell(int viewId)
    {
        photonView.RPC(nameof(RpcRequestSell), RpcTarget.MasterClient, viewId);
    }

    [PunRPC]
    private void RpcRequestSell(int viewId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(viewId);
        if (pv == null) return;

        StructureSelectable selectable = pv.GetComponent<StructureSelectable>();
        if (selectable != null && selectable.type != null)
        {
            if (!selectable.CanSell)
                return;

            int refund = Mathf.RoundToInt(selectable.type.cost * 0.3f);
            ResourceNet.Instance?.MasterAddMoney(refund);
        }

        PhotonNetwork.Destroy(pv.gameObject);
    }

    public void RequestRepair(int viewId)
    {
        photonView.RPC(nameof(RpcRequestRepair), RpcTarget.MasterClient, viewId);
    }

    [PunRPC]
    private void RpcRequestRepair(int viewId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(viewId);
        if (pv == null) return;

        StructureSelectable selectable = pv.GetComponent<StructureSelectable>();
        if (selectable == null || selectable.type == null) return;
        if (!selectable.CanRepair) return;

        BuildingHealthNet health = pv.GetComponent<BuildingHealthNet>();
        if (health == null) return;

        if (health.CurrentHp >= health.MaxHp) return;

        if (ResourceNet.Instance == null) return;
        if (!ResourceNet.Instance.MasterTrySpendMoney(selectable.type.repairCost)) return;

        health.MasterRepair(selectable.type.repairAmount);
    }

    private SupporterItemSO GetSupportItem(int supportTypeIndex)
    {
        if (supportItemTypes == null || supportTypeIndex < 0 || supportTypeIndex >= supportItemTypes.Length)
            return null;

        return supportItemTypes[supportTypeIndex];
    }

    private bool CanPlacePurificationBeacon(SupporterItemSO item, Vector3 position, int requesterActor)
    {
        CleanupExpiredPurificationBeacons();
        PurificationBeaconSupportEffectSO beaconEffect = GetPurificationBeaconEffect(item);
        if (beaconEffect == null)
            return false;

        if (!IsSupporterRequester(requesterActor))
            return false;

        if (nextBeaconPlaceTimes.TryGetValue(item.typeId, out float nextPlaceTime) && Time.time < nextPlaceTime)
            return false;

        if (activeBeaconExpireTimes.Count >= Mathf.Max(1, item.maxActiveCount))
            return false;

        GameObject shooter = GameObject.FindGameObjectWithTag("Player");
        if (shooter == null)
            return false;

        return Vector3.Distance(shooter.transform.position, position) <= Mathf.Max(0f, beaconEffect.placementRangeFromShooter);
    }

    private void RegisterPurificationBeaconPlacement(SupporterItemSO item)
    {
        PurificationBeaconSupportEffectSO beaconEffect = GetPurificationBeaconEffect(item);
        if (beaconEffect == null)
            return;

        nextBeaconPlaceTimes[item.typeId] = Time.time + Mathf.Max(0f, item.cooldown);
        activeBeaconExpireTimes.Add(Time.time + Mathf.Max(0.1f, beaconEffect.activeDuration));
    }

    private void CleanupExpiredPurificationBeacons()
    {
        for (int i = activeBeaconExpireTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeBeaconExpireTimes[i])
                activeBeaconExpireTimes.RemoveAt(i);
        }
    }

    private bool IsSupporterRequester(int requesterActor)
    {
        if (!PhotonNetwork.InRoom)
            return true;

        Player requester = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.GetPlayer(requesterActor) : null;
        if (requester == null)
            return false;

        return requester.CustomProperties.TryGetValue("Role", out object roleValue) && roleValue as string == "Supporter";
    }

    private PurificationBeaconSupportEffectSO GetPurificationBeaconEffect(SupporterItemSO item)
    {
        return item != null ? item.effect as PurificationBeaconSupportEffectSO : null;
    }
}
