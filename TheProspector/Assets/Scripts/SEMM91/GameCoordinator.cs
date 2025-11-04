using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameCoordinator : NetworkBehaviour
{
    public static GameCoordinator Instance;

    // Who is the current keeper (owner under DA semantics for this demo)
    public NetworkVariable<ulong> KeeperClientId = new(0, NetworkVariableReadPermission.Everyone);

    // Simple replicated round/turn state for display only
    public NetworkVariable<int> GlobalTurn = new(0, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> RoundIndex = new(0, NetworkVariableReadPermission.Everyone);

    // Server-only state
    private readonly Dictionary<ulong, int> _turns = new();   // per-player 0..4
    private readonly Dictionary<ulong, int> _points = new();  // per-player accumulation this round

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // If host started alone, keeper defaults to host until 2 clients join
            KeeperClientId.Value = OwnerClientId;
            TryStartWhenThree();
        }
    }

    private void OnClientConnected(ulong id)
    {
        _turns.TryAdd(id, 0);
        _points.TryAdd(id, 0);
        TryStartWhenThree();
    }

    private void OnClientDisconnected(ulong id)
    {
        _turns.Remove(id);
        _points.Remove(id);
        // MVP: do nothing special — this demo focuses purely on “keeper migration when all 3 are present”.
    }

    private void TryStartWhenThree()
    {
        if (!IsServer) return;
        if (NetworkManager.ConnectedClientsIds.Count < 3) return;

        // Randomize first keeper only once at the start of round 0
        if (RoundIndex.Value == 0 && GlobalTurn.Value == 0 && _initializedRound0 == false)
        {
            var ids = NetworkManager.ConnectedClientsIds.ToList();
            var pick = ids[Random.Range(0, ids.Count)];
            SetKeeper(pick);
            _initializedRound0 = true;
            BroadcastStateClientRpc();
        }
    }
    private bool _initializedRound0 = false;

    private void SetKeeper(ulong newKeeper)
    {
        KeeperClientId.Value = newKeeper;

        // Transfer ownership of this coordinator object — key to DA “host migration” demo
        if (NetworkObject.OwnerClientId != newKeeper)
            NetworkObject.ChangeOwnership(newKeeper);

        // Reset server-only round state
        foreach (var id in NetworkManager.ConnectedClientsIds)
        {
            _turns[id] = 0;
            _points[id] = 0;
        }
        GlobalTurn.Value = 0;
    }

    // Called by ServerRpc from players
    public void RegisterTurn(ulong senderClientId)
    {
        if (!IsServer) return;
        if (!NetworkManager.ConnectedClientsIds.Contains(senderClientId)) return;

        // cap at 4 presses per round
        if (_turns[senderClientId] >= 4) return;

        // Points: Keeper=1, Regulars=2 or 3 (stable mapping by smallest clientId gets 3)
        int add = (senderClientId == KeeperClientId.Value) ? 1 : PointsForRegular(senderClientId);
        _points[senderClientId] += add;
        _turns[senderClientId] += 1;

        // GlobalTurn is the max local turn any player has reached (0..4)
        GlobalTurn.Value = Mathf.Min(4, _turns.Values.Max());

        BroadcastStateClientRpc();

        // If all players reached 4 turns, resolve round and migrate keeper
        if (AllPlayersAtFour())
        {
            ResolveRoundAndMigrate();
        }
    }

    private int PointsForRegular(ulong clientId)
    {
        // Among non-keeper players: lowest clientId gets 3, the other gets 2
        var regs = NetworkManager.ConnectedClientsIds
            .Where(id => id != KeeperClientId.Value)
            .OrderBy(id => id).ToList();
        if (regs.Count < 2) return 2; // degenerate case
        return (clientId == regs[0]) ? 3 : 2;
    }

    private bool AllPlayersAtFour()
    {
        if (NetworkManager.ConnectedClientsIds.Count < 3) return false;
        foreach (var id in NetworkManager.ConnectedClientsIds)
        {
            if (!_turns.TryGetValue(id, out var t) || t < 4) return false;
        }
        return true;
    }

    private void ResolveRoundAndMigrate()
    {
        // Choose highest scorer as next keeper
        var winner = _points.OrderByDescending(kv => kv.Value).First().Key;

        RoundIndex.Value += 1;
        SetKeeper(winner);
        BroadcastStateClientRpc();
    }

    [ClientRpc]
    private void BroadcastStateClientRpc()
    {
        // For MVP, just UI text is enough; no per-client data push needed beyond NetworkVariables
    }

    private void OnGUI()
    {
        // Simple HUD visible to everyone
        GUILayout.BeginArea(new Rect(10, 10, 420, 180));
        GUILayout.Label($"Round: {RoundIndex.Value}   Turn: {GlobalTurn.Value}/4");
        GUILayout.Label($"Keeper (owner of coordinator): {KeeperClientId.Value}");
        GUILayout.Label($"Press SPACE to score. Keeper=1, Others=2/3.");
        GUILayout.EndArea();
    }
}
