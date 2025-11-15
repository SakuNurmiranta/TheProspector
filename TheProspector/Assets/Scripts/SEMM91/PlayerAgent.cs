using SEMM91;
using SEMM91.Networking;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Serialization;

namespace SEMM91
{
    public class PlayerAgent : NetworkBehaviour
{
    [FormerlySerializedAs("PlayerName")] public NetworkVariable<FixedString32Bytes> playerName =
        new("Player", NetworkVariableReadPermission.Everyone);

    private NetPlayerState _state;

    private void Awake()
    {
        _state = GetComponent<NetPlayerState>();
        if (_state == null)
        {
            Debug.LogError("PlayerAgent requires a NetPlayerState component");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            playerName.Value = $"P{NetworkManager.Singleton.LocalClientId}";
        }
    }
    
    // private void Start()
    // {
    //     if (!IsOwner) return;
    //     // simple default name per window; you can change it in inspector per instance if you want
    //     playerName.Value = $"P{NetworkManager.Singleton.LocalClientId}";
    // }

    private void Update()
    {
        if (!IsOwner || !IsClient) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SubmitEndTurnServerRpc(); // same as before
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            SubmitSkipTurnServerRpc();
        }
        
        
    }

    [ServerRpc]
    private void SubmitEndTurnServerRpc(ServerRpcParams p = default)
    {
        var g = GameCoordinator.Instance;
        if (g == null) return;
        g.RegisterEndTurn(p.Receive.SenderClientId);
    }
    
    [ServerRpc]
    private void SubmitSkipTurnServerRpc(ServerRpcParams p = default)
    {
        var g = GameCoordinator.Instance;
        if (g == null) return;
        g.RegisterSkipTurn(p.Receive.SenderClientId);
    }

    private void OnGUI()
    {
        if (!IsOwner) return;

        var g = GameCoordinator.Instance;
        if (g == null) return;
        
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        ulong keeper = g.keeperClientId.Value;
        bool isKeeper = myClientId == keeper;
        
        string role = isKeeper ? "Keeper" : "Regular";
        
        string traitsText = "No NetPlayerState";

        if (_state != null)
        {
            traitsText =
                $"Score: {_state.ScoreValue} | " +
                $"Exhausted: {_state.ExhaustedValue} | " +
                $"Active: {_state.ActiveValue} |";
        }


        float y = 10 + 90 * (int)myClientId;
        GUI.Label(new Rect(10, y, 380, 20),
            $"{playerName.Value} (ClientId {myClientId})");
        GUI.Label(new Rect(10, y + 20, 380, 20),
            $"Role: {role} | {traitsText}");
        GUI.Label(new Rect(10, y + 40, 380, 20),
            "SPACE = End Turn | BACKSPACE = Skip Turn");
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
}

