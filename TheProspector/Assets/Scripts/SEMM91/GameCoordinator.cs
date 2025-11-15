using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using SEMM91.Networking;
using UnityEngine.Serialization; // access NEtPlayerState

namespace SEMM91
{
    public class GameCoordinator : NetworkBehaviour
    {
        public static GameCoordinator Instance;

        // Just a role for now
        [FormerlySerializedAs("KeeperClientId")] public NetworkVariable<ulong> keeperClientId = new();

        // Simple replicated year/season indexes
        [FormerlySerializedAs("GlobalTurn")] public NetworkVariable<int> globalTurn = new();
        [FormerlySerializedAs("RoundIndex")] public NetworkVariable<int> roundIndex = new();

        private const int TurnsPerYear = 4;

        // Server-only state
        // which players have acted already
        private readonly HashSet<ulong> _actedThisTurn = new();

        //cached mapping for client states
        private readonly Dictionary<ulong, NetPlayerState> _playerStates = new();

        bool _gameEnded;
        readonly ulong _finalWinner = ulong.MaxValue;

        private void Awake() => Instance = this;

        private void Update()
        {
            if (!_gameEnded) return;

            // Press Escape in any window to quit
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                NetworkManager.Singleton.Shutdown();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // seed for already-connected clients (incl. host)
                foreach (var id in NetworkManager.ConnectedClientsIds)
                {
                    RegisterPlayerServer(id);
                }

                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

                keeperClientId.Value = OwnerClientId; // default to host
                TryStartWhenThree();
            }
        }

        private new void OnDestroy()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        // discover/register player upon connection
        private void RegisterPlayerServer(ulong clientId)
        {
            if (!IsServer) return;

            if (!_playerStates.ContainsKey(clientId))
            {
                if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    var playerObj = client.PlayerObject;
                    if (playerObj != null && playerObj.TryGetComponent(out NetPlayerState state))
                    {
                        _playerStates[clientId] = state;
                        // ensure defaults in host-authoritative mode
                        var index = _playerStates.Count - 1;
                        state.InitializeServer(index, $"Player {clientId}");
                    }
                    else
                    {
                        Debug.LogWarning($"GameCoordinator: No NetPlayerState on player object for client {clientId}");
                    }
                }
            }

            //force per-turn logic
            _actedThisTurn.Remove(clientId);
        }

        private void OnClientConnected(ulong id)
        {
            RegisterPlayerServer(id);
            TryStartWhenThree();
        }

        private void OnClientDisconnected(ulong id)
        {
            _actedThisTurn.Remove(id);
            _playerStates.Remove(id);
            // MVP: do nothing special — this demo focuses purely on “keeper migration when all 3 are present”.
            EnsureKeeperSelected();
        }

        private void TryStartWhenThree()
        {
            if (!IsServer) return;
            if (NetworkManager.ConnectedClientsIds.Count < 3) return;

            globalTurn.Value = 0;
            roundIndex.Value = 0;
            _actedThisTurn.Clear();

            EnsureKeeperSelected();
            BroadcastStateClientRpc();
        }

        private void SetKeeper(ulong newKeeper)
        {
            //at this stage, this is simply a ritual
            keeperClientId.Value = newKeeper;
            _actedThisTurn.Clear();


        }

        private void EnsureKeeperSelected()
        {
            // Build candidate set: all active players
            var candidates = _playerStates
                .Where(kvp => kvp.Value != null && kvp.Value.ActiveValue)
                .Select(kvp => kvp.Key)
                .OrderBy(id => id) // deterministic order: lowest clientId first
                .ToList();

            if (candidates.Count == 0)
            {
                // No active players -> no meaningful keeper. 
                // You can keep the old value or use a sentinel.
                // For now, we just leave keeperClientId as-is.
                return;
            }

            // If current keeper is still valid & active, keep them.
            if (candidates.Contains(keeperClientId.Value))
                return;

            // Otherwise, elect a new keeper deterministically.
            ulong newKeeper = candidates[0]; // lowest active clientId
            SetKeeper(newKeeper);
        }
        
        // -- Public API for turn actions
        public void RegisterEndTurn(ulong senderClientId)
        {
            if (!IsServer) return;
            if (!NetworkManager.ConnectedClientsIds.Contains(senderClientId)) return;

            if (!TryGetPlayerState(senderClientId, out var state)) return;

            if (!CanActThisTurn(senderClientId, state))
                return;

            state.AddToScoreServer(1);
            state.SetExhaustedServer(true);

            MarkActedAndAdvanceIfReady(senderClientId);
        }

        public void RegisterSkipTurn(ulong senderClientId)
        {
            if (!IsServer) return;
            if (!NetworkManager.ConnectedClientsIds.Contains(senderClientId)) return;
            if (!TryGetPlayerState(senderClientId, out var state)) return;
            if (!CanActThisTurn(senderClientId, state)) return;

            //Apply Skip Turn
            state.SetExhaustedServer(false); //recovery

            MarkActedAndAdvanceIfReady(senderClientId);
        }

        private bool TryGetPlayerState(ulong clientId, out NetPlayerState state)
        {
            if (_playerStates.TryGetValue(clientId, out state) && state != null) return true;

            //fallback
            if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var playerObj = client.PlayerObject;
                if (playerObj != null && playerObj.TryGetComponent(out state))
                {
                    _playerStates[clientId] = state;
                    return true;
                }
            }

            state = null;
            return false;
        }

        // a player can act if they haven't acted yet this turn and they are active (IsActive)
        private bool CanActThisTurn(ulong clientID, NetPlayerState state)
        {
            if (_actedThisTurn.Contains(clientID))
                return false;
            if (!state.ActiveValue)
                return false;

            return true;
        }

        private void MarkActedAndAdvanceIfReady(ulong senderClientId)
        {
            _actedThisTurn.Add(senderClientId);

            if (AllActivePlayersActed())
            {
                AdvanceGlobalTurn();
            }
            else
            {
                BroadcastStateClientRpc();
            }
        }

        private bool AllActivePlayersActed()
        {
            foreach (var kvp in _playerStates)
            {
                var clientId = kvp.Key;
                var state = kvp.Value;
                if (state == null) continue;

                if (!state.ActiveValue) continue;

                if (!_actedThisTurn.Contains(clientId)) return false;

            }

            return true;

        }

        private void AdvanceGlobalTurn()
        {
            // clear actions for next turn
            _actedThisTurn.Clear();

            globalTurn.Value++;

            //increment year in four season cycles
            if (globalTurn.Value > 0 && globalTurn.Value % TurnsPerYear == 0)
            {
                roundIndex.Value++;

                //TO DO: Hook up keeper validity check & tally updates
            }

            // NEW: pick a Keeper deterministically when entering turn 1
            EnsureKeeperSelected();
            
            BroadcastStateClientRpc();
            
        }

        /*private void EndGame(ulong winner)
        {
            _gameEnded = true;
            _finalWinner = winner;

            // Freeze all scoring
            keeperClientId.Value = winner;

            // Stop turns from changing further
            //GlobalTurn.Value = 4;

            BroadcastStateClientRpc();
        }*/

        [ClientRpc]
        private void BroadcastStateClientRpc()
        {
            // For MVP, just UI text is enough; no per-client data push needed beyond NetworkVariables
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 80));
            GUILayout.Label($"Year: {roundIndex.Value}   Turn: {globalTurn.Value} (global)");
            GUILayout.Label($"Keeper (role, not owner): {keeperClientId.Value}");
            GUILayout.Label("SPACE = End Turn (score++, exhausted=true)");
            GUILayout.Label("BACKSPACE = Skip Turn (score stays, exhausted=false)");
            GUILayout.EndArea();

            if (_gameEnded)
            {
                GUILayout.BeginArea(new Rect(10, 100, 400, 100));
                GUILayout.Label($"GAME OVER — Winner: Client {_finalWinner}");
                GUILayout.Label("Press ESC to quit");
                GUILayout.EndArea();
            }
        }
    }
}