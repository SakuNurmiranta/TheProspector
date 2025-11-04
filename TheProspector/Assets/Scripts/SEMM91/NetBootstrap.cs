using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

#if USE_DA || USE_RELAY
using Unity.Services.Core;
using Unity.Services.Authentication;
#endif

#if USE_RELAY
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
#endif

public class NetBootstrap : MonoBehaviour
{
    [Header("Managers in Scene (assign in Inspector)")]
    [SerializeField] private NetworkManager localManager;
    [SerializeField] private UnityTransport localTransport;

    [SerializeField] private NetworkManager daManager;
    [SerializeField] private UnityTransport daTransport;
    [SerializeField] private GameObject daManagerPrefab;
    [SerializeField] private GameObject localManagerPrefab;
    
    [Header("Game Coordinator Prefab (assign)")]
    [SerializeField] private GameObject gameCoordinatorPrefab;
    private GameObject _spawnedCoordinator;

    [Header("Local (non-DA)")]
    public string localAddress = "127.0.0.1";
    public ushort localPort = 7777;

    [Header("DA/Relay Session")]
    [Tooltip("For Relay: host shows a join code; clients paste it here before pressing DA Client.")]
    public string daSessionName = "test-session"; // for Relay clients: set this to join code
    public int daCapacity = 3;

    private NetworkManager activeNM;
    private UnityTransport activeUTP;

    enum Topology { Local, DA }

    void Awake()
    {
        // Ensure only one is active at boot (choose local by default)
        SetActiveManager(Topology.Local, activateOnly: false);
    }

    // ---------- PUBLIC BUTTONS ----------
    public void StartLocalHost()
    {
        SetActiveManager(Topology.Local);
        RegisterCoordinatorPrefab(activeNM);

        activeUTP.SetConnectionData(localAddress, localPort);
        activeNM.StartHost();

        SpawnCoordinatorIfHost();
        Debug.Log("[BOOT] Local Host started");
    }

    public void StartLocalClient()
    {
        SetActiveManager(Topology.Local);
        RegisterCoordinatorPrefab(activeNM);

        activeUTP.SetConnectionData(localAddress, localPort);
        activeNM.StartClient();

        Debug.Log("[BOOT] Local Client started");
    }

    public async void StartDAHost()
    {
        SetActiveManager(Topology.DA);
        await StartDAInternal(isHost: true);
    }

    public async void StartDAClient()
    {
        SetActiveManager(Topology.DA);
        await StartDAInternal(isHost: false);
    }

    // ---------- CORE ----------
    private void SetActiveManager(Topology topo, bool activateOnly = true)
    {
        if (localManager) localManager.gameObject.SetActive(false);
        if (daManager)    daManager.gameObject.SetActive(false);

        NetworkManager targetNM;
        UnityTransport targetUTP;

        if (topo == Topology.Local)
        {
            if (!localManager || !localTransport)
            {
                if (localManager == null && localManagerPrefab != null)
                {
                    var go = Instantiate(localManagerPrefab);
                    go.name = "NetworkManager_Local (Runtime)";
                    localManager = go.GetComponent<NetworkManager>();
                    localTransport = go.GetComponent<UnityTransport>() 
                                     ?? go.GetComponentInChildren<UnityTransport>(true);
                }
            }

            if (!localManager || !localTransport)
                throw new Exception("[BOOT] Local manager/transport not assigned and no prefab provided.");

            targetNM  = localManager;
            targetUTP = localTransport;
        }
        else // DA
        {
            // If not assigned in inspector, try to instantiate from prefab
            if (!daManager || !daTransport)
            {
                if (daManager == null && daManagerPrefab != null)
                {
                    var go = Instantiate(daManagerPrefab);
                    go.name = "NetworkManager_DA (Runtime)";
                    daManager = go.GetComponent<NetworkManager>();
                    daTransport = go.GetComponent<UnityTransport>() 
                                  ?? go.GetComponentInChildren<UnityTransport>(true);
                }
            }

            if (!daManager || !daTransport)
                throw new Exception("[BOOT] DA manager/transport not assigned and no prefab provided.");

            targetNM  = daManager;
            targetUTP = daTransport;
        }

        EnsureSingleton(targetNM);
        targetNM.gameObject.SetActive(true);

        activeNM  = targetNM;
        activeUTP = targetUTP;

        if (!activateOnly)
        {
            // just switching; not starting yet
        }
    }

    private void EnsureSingleton(NetworkManager desired)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton != desired)
        {
            var other = NetworkManager.Singleton;
            if (other.IsListening)
            {
                try { other.Shutdown(); } catch { /* ignore */ }
            }
            Destroy(other.gameObject);
        }
    }

    private void RegisterCoordinatorPrefab(NetworkManager nm)
    {
        if (!gameCoordinatorPrefab) throw new Exception("[BOOT] GameCoordinator prefab not assigned.");
        foreach (var p in nm.NetworkConfig.Prefabs.Prefabs)
            if (p.Prefab == gameCoordinatorPrefab) return;

        nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = gameCoordinatorPrefab });
    }

    private void SpawnCoordinatorIfHost()
    {
        if (activeNM != null && activeNM.IsHost && _spawnedCoordinator == null)
        {
            _spawnedCoordinator = Instantiate(gameCoordinatorPrefab);
            var no = _spawnedCoordinator.GetComponent<NetworkObject>();
            no.Spawn();
        }
    }

    private async Task StartDAInternal(bool isHost)
    {
#if USE_RELAY || USE_DA
        try
        {
            RegisterCoordinatorPrefab(activeNM);
            await EnsureServicesAsync();

            bool joined =
#if USE_RELAY
                (isHost ? await DAHelpers.CreateOrGetSessionAsync(daSessionName, daCapacity)
                        : await DAHelpers.JoinSessionAsync(daSessionName));
#else // USE_DA only
                (isHost ? await DAHelpers.CreateOrGetSessionAsync(daSessionName, daCapacity)
                        : await DAHelpers.JoinSessionAsync(daSessionName));
#endif

            if (!joined)
            {
                Debug.LogWarning("[BOOT][DA] Session not ready. Falling back to LOCAL.");
                FallbackToLocal(isHost);
                return;
            }

            await DAHelpers.ConfigureTransportForCurrentSession(activeUTP);

            if (isHost) activeNM.StartHost();
            else        activeNM.StartClient();

            SpawnCoordinatorIfHost();
            Debug.Log($"[BOOT][DA] Started as {(isHost ? "Host" : "Client")} via {(IsRelayEnabled ? "Relay" : "DA")}.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BOOT][DA] Exception: {ex.Message}\nFalling back to LOCAL.");
            FallbackToLocal(isHost);
        }
#else
        Debug.LogWarning("[BOOT][DA] Neither USE_RELAY nor USE_DA defined; starting LOCAL instead.");
        FallbackToLocal(isHost);
#endif
    }

    private void FallbackToLocal(bool isHost)
    {
        SetActiveManager(Topology.Local);
        RegisterCoordinatorPrefab(activeNM);

        activeUTP.SetConnectionData(localAddress, localPort);
        if (isHost) activeNM.StartHost();
        else        activeNM.StartClient();

        SpawnCoordinatorIfHost();
        Debug.Log("[BOOT] Fallback to Local started");
    }

#if USE_RELAY || USE_DA
    private static async Task EnsureServicesAsync()
    {
        if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
#endif

#if USE_RELAY
    
    // -------------------- RELAY IMPLEMENTATION --------------------
    static class DAHelpers
{
    private static Allocation _hostAlloc;
    private static JoinAllocation _clientAlloc;

    public static async Task<bool> CreateOrGetSessionAsync(string sessionName, int capacity)
    {
        _hostAlloc = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(2, capacity));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(_hostAlloc.AllocationId);
        Debug.Log($"[RELAY] Join Code: {joinCode} — paste into NetBootstrap.daSessionName on clients.");
        return true;
    }

    public static async Task<bool> JoinSessionAsync(string sessionNameOrJoinCode)
    {
        var code = sessionNameOrJoinCode?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("[RELAY] Join code is empty (daSessionName).");
            return false;
        }
        _clientAlloc = await RelayService.Instance.JoinAllocationAsync(code);
        return true;
    }

    public static async Task ConfigureTransportForCurrentSession(UnityTransport utp)
    {
        await Task.CompletedTask;

        if (_hostAlloc != null)
        {
            // Host: use ConnectionData for BOTH connectionData and hostConnectionData
            var host = _hostAlloc.RelayServer.IpV4;
            var port = (ushort)_hostAlloc.RelayServer.Port;

            var data = new RelayServerData(
                host, port,
                _hostAlloc.AllocationIdBytes,
                _hostAlloc.ConnectionData,
                _hostAlloc.Key,
                _hostAlloc.ConnectionData,   // hostConnectionData for host
                true,                        // secure (DTLS)
                true                         // isRelay (older API overload)
            );

            // If your UnityTransport requires 'ref', use the ref version instead of the value version:
            // utp.SetRelayServerData(ref data);
            utp.SetRelayServerData(data);
            return;
        }

        if (_clientAlloc != null)
        {
            var host = _clientAlloc.RelayServer.IpV4;
            var port = (ushort)_clientAlloc.RelayServer.Port;

            var data = new RelayServerData(
                host, port,
                _clientAlloc.AllocationIdBytes,
                _clientAlloc.ConnectionData,
                _clientAlloc.Key,
                _clientAlloc.HostConnectionData, // provided by JoinAllocation
                true,                            // secure (DTLS)
                true
            );

            // If your UnityTransport requires 'ref', use the ref version instead of the value version:
            // utp.SetRelayServerData(ref data);
            utp.SetRelayServerData(data);
            return;
        }

        throw new Exception("[RELAY] No allocation found. Call CreateOrGetSessionAsync/JoinSessionAsync first.");
    }
}

private static bool IsRelayEnabled => true;
#elif USE_DA
    // -------------------- REAL DA (fill when ready) --------------------
    static class DAHelpers
    {
        public static async Task<bool> CreateOrGetSessionAsync(string sessionName, int capacity)
        {
            // TODO: Replace with DA SDK session create/list/join.
            await Task.CompletedTask; return true;
        }

        public static async Task<bool> JoinSessionAsync(string sessionName)
        {
            // TODO: Replace with DA SDK join by name/id.
            await Task.CompletedTask; return true;
        }

        public static async Task ConfigureTransportForCurrentSession(UnityTransport utp)
        {
            // TODO: If DA requires endpoint/payload, configure UTP here.
            await Task.CompletedTask;
        }
    }

    private static bool IsRelayEnabled => false;
#endif
}
