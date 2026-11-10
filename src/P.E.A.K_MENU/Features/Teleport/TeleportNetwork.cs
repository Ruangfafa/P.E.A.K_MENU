using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
namespace P.E.A.K_MENU.Features.Teleport;
internal static class TeleportNetwork
{
    private const byte RequestCode = 193;
    private const string Key = "peakmenu.freeTeleport.v1";
    private const string ReductionKey = "peakmenu.statusReduction.v1";
    internal static bool HostSupportsReduction => PhotonNetwork.InRoom && PhotonNetwork.MasterClient != null && PhotonNetwork.MasterClient.CustomProperties[ReductionKey] is bool supported && supported;
    internal static bool HostSupportsTeleport => PhotonNetwork.InRoom && PhotonNetwork.MasterClient != null && PhotonNetwork.MasterClient.CustomProperties[Key] is bool;
    internal static bool Free => PhotonNetwork.MasterClient != null && PhotonNetwork.MasterClient.CustomProperties[Key] is bool value && value;
    internal static bool CanBring => PhotonNetwork.IsMasterClient || Free;
    internal static void Initialize() { PhotonNetwork.NetworkingClient.EventReceived += Receive; }
    internal static void Dispose()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= Receive;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { [ReductionKey] = false, [Key] = null });
    }
    internal static void Update()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && !(PhotonNetwork.LocalPlayer.CustomProperties[Key] is bool)) SetFree(true);
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && !HostSupportsReduction)
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { [ReductionKey] = true });
    }
    internal static void SetFree(bool value)
    {
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { [Key] = value });
    }
    internal static bool Request(int target)
    {
        return Free && PhotonNetwork.RaiseEvent(RequestCode, new object[] { "peakmenu.teleport.v1", target }, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    internal static bool RequestStatusReduction(Character target, CharacterAfflictions.STATUSTYPE type, float amount)
    {
        if (!HostSupportsReduction) return false;
        return PhotonNetwork.RaiseEvent(194, new object[] { "peakmenu.reduce.v1", target.photonView.ViewID, (int)type, amount },
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    private static void Receive(EventData ev)
    {
        if (ev.Code == 194 && PhotonNetwork.IsMasterClient)
        {
            if (!(ev.CustomData is object[] request) || request.Length != 4 || !(request[0] is string tag) || tag != "peakmenu.reduce.v1" ||
                !(request[1] is int targetId) || !(request[2] is int status) || !(request[3] is float amount) ||
                float.IsNaN(amount) || float.IsInfinity(amount) || amount >= 0f || amount < -2f) return;
            bool senderPresent = false;
            foreach (var character in Resources.FindObjectsOfTypeAll<Character>())
                if (character != null && character.gameObject.activeInHierarchy && character.IsPlayerControlled && character.IsRegisteredToPlayer && character.photonView.OwnerActorNr == ev.Sender) senderPresent = true;
            var targetView = PhotonView.Find(targetId);
            var reductionTarget = targetView == null ? null : targetView.GetComponent<Character>();
            if (!senderPresent || reductionTarget == null || !reductionTarget.IsPlayerControlled || !reductionTarget.IsRegisteredToPlayer || !reductionTarget.gameObject.activeInHierarchy) return;
            var type = (CharacterAfflictions.STATUSTYPE)status;
            if (type != CharacterAfflictions.STATUSTYPE.Injury && type != CharacterAfflictions.STATUSTYPE.Hunger &&
                type != CharacterAfflictions.STATUSTYPE.Poison && type != CharacterAfflictions.STATUSTYPE.Cold &&
                type != CharacterAfflictions.STATUSTYPE.Hot && type != CharacterAfflictions.STATUSTYPE.Drowsy &&
                type != CharacterAfflictions.STATUSTYPE.Curse && type != CharacterAfflictions.STATUSTYPE.Spores && type != CharacterAfflictions.STATUSTYPE.Petrify) return;
            var changes = new float[CharacterAfflictions.NumStatusTypes];
            changes[status] = amount;
            targetView!.RPC("RPC_ApplyStatusesFromFloatArray", RpcTarget.All, new object[] { changes });
            return;
        }
        if (ev.Code != RequestCode || !PhotonNetwork.IsMasterClient || !Free || !(ev.CustomData is object[] data) || data.Length != 2 || !(data[0] is string protocol) || protocol != "peakmenu.teleport.v1" || !(data[1] is int viewId)) return;
        Character? requester = null;
        foreach (var character in Resources.FindObjectsOfTypeAll<Character>())
            if (character != null && character.gameObject.activeInHierarchy && character.IsPlayerControlled && character.IsRegisteredToPlayer && character.photonView.OwnerActorNr == ev.Sender) requester = character;
        var view = PhotonView.Find(viewId);
        var target = view == null ? null : view.GetComponent<Character>();
        if (requester == null || target == null || !target.IsPlayerControlled || !target.IsRegisteredToPlayer || !target.gameObject.activeInHierarchy) return;
        target.photonView.RPC("WarpPlayerRPC", RpcTarget.All, TeleportService.CalculateArrivalPosition(requester, target), false);
    }
}
