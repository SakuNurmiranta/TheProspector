using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerAgent : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName =
        new("Player", NetworkVariableReadPermission.Everyone);

    private void Start()
    {
        if (!IsOwner) return;
        // simple default name per window; you can change it in inspector per instance if you want
        PlayerName.Value = $"P{NetworkManager.Singleton.LocalClientId}";
    }

    private void Update()
    {
        if (!IsOwner || !IsClient) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SubmitTurnServerRpc(); // same as before
        }
        
        
    }

    [ServerRpc]
    private void SubmitTurnServerRpc(ServerRpcParams p = default)
    {
        var g = GameCoordinator.Instance;
        if (g == null) return;
        g.RegisterTurn(p.Receive.SenderClientId);
    }

    private void OnGUI()
    {
        if (!IsOwner) return;

        var g = GameCoordinator.Instance;
        var keeper = g != null ? g.KeeperClientId.Value : ulong.MaxValue;
        bool isKeeper = NetworkManager.Singleton.LocalClientId == keeper;

        string role = isKeeper ? "Keeper" : "Regular";
        int weight = isKeeper ? 1 : PointsForRegularLocal(keeper);

        float y = 10 + 90 * (int)NetworkManager.Singleton.LocalClientId;
        GUI.Label(new Rect(10, y, 380, 20), $"{PlayerName.Value} (ClientId {NetworkManager.Singleton.LocalClientId})");
        GUI.Label(new Rect(10, y + 20, 380, 20), $"Role: {role}  |  Space adds: +{weight} pts");
        GUI.Label(new Rect(10, y + 40, 380, 20), "Press SPACE to score");
    }

    int PointsForRegularLocal(ulong keeperId)
    {
        // mirror the server's deterministic mapping (lowest regular id gets 3, the other gets 2)
        var ids = NetworkManager.Singleton.ConnectedClientsIds;
        ulong low = ulong.MaxValue, high = ulong.MaxValue;
        foreach (var id in ids)
        {
            if (id == keeperId) continue;
            if (low == ulong.MaxValue || id < low) { high = low; low = id; }
            else if (high == ulong.MaxValue || id < high) { high = id; }
        }
        var me = NetworkManager.Singleton.LocalClientId;
        if (me == keeperId) return 1;
        return me == low ? 3 : 2;
    }
}
