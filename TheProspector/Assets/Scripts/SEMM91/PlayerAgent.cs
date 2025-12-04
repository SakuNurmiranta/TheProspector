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
    
}
}

