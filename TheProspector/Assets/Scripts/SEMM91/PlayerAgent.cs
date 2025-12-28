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
    
    // Bot settings
    private bool _botMode;
    private bool _botStress;
    private int _botSeed;
    private System.Random _rng;

    private float _nextActionAt;
    private float _burstEndsAt;
    private bool _inBurst;

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
        
        //bot config available if local owner client
        if (IsOwner && IsClient)
        {
            _botMode = BotConfig.HasArg("-bot");
            _botStress = BotConfig.HasArg("-botStress");
            _botSeed = BotConfig.GetIntArg("-botSeed", 12345) + (int)NetworkManager.Singleton.LocalClientId;
            _rng = new System.Random(_botSeed);
            
            if (_botMode)
            {
                ScheduleNextAction();
                Debug.Log($"[BOT] Enabled. stress={_botStress} seed={_botSeed} clientId={NetworkManager.Singleton.LocalClientId}");
            }

            var gc = GameCoordinator.Instance;
            if (gc != null)
            {
                gc.ReportClientReadyServerRpc();
            }
        }
        
        RunLog.Header(
            role: "client",
            testCase: BotConfig.GetStringArg("-tc", "TC-UNKNOWN"),
            preset: BotConfig.GetStringArg("-netPreset", "P?-UNKNOWN"),
            clientsPlanned: BotConfig.GetIntArg("-clients", 6),
            botSeed: _botSeed
        );
        
    }

    private void Update()
    {
        if (!IsOwner || !IsClient) return;

        if (_botMode)
        {
            var gc = GameCoordinator.Instance;
            if (gc == null || !gc.testStarted.Value) 
                return;
            
            BotTick();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SubmitEndTurnServerRpc(); // same as before
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            SubmitSkipTurnServerRpc();
        }
        
        
    }

    private void BotTick()
    {
        if (_botStress && !_inBurst)
        {
            if (_rng.NextDouble() < 0.02)
            {
                _inBurst = true;
                _burstEndsAt = Time.time + 10f;
                ScheduleNextAction(burst: true);
            }
        }
        
        if (_inBurst && Time.time >= _burstEndsAt)
        {
            _inBurst = false;
            ScheduleNextAction();
        }
        
        double roll = _rng.NextDouble();
        
        if (roll < 0.65)
        {
            SubmitEndTurnServerRpc();
            Debug.Log($"[BOT] Space -> End turn t={Time.time:F2}");
        }
        else if (roll < 0.95)
        {
            SubmitSkipTurnServerRpc();
            Debug.Log($"[BOT] Backspace -> Skip turn t={Time.time:F2}");
        }
        else 
        {
           //idle
           Debug.Log($"[BOT] Idle t={Time.time:F2}");
        }
        
        ScheduleNextAction(burst: _inBurst);
        
    }

    private void ScheduleNextAction(bool burst = false)
    {
        float min = burst ? 0.08f : 0.25f;
        float max = burst ? 0.20f : 0.80f;
        float dt = (float)(min + _rng.NextDouble() * (max - min));
        _nextActionAt = Time.time + dt;
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

