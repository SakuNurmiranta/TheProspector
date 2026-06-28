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

namespace SEMM91
{
    public class NetBootstrap : MonoBehaviour
    {
        [Header("Managers in Scene (assign in Inspector)")] [SerializeField]
        private NetworkManager localManager;

        private int _targetFps;
        
        private enum AutoMode {None, Server, Client, Host}
        private AutoMode _autoMode = AutoMode.None;
        private bool _botMode;
        private bool _botExitOnDisconnect;
        private bool _applicationIsQuitting;
        private int _botMinMs = 100;
        private int _botMaxMs = 400;

        private int _simDelayMs;
        private int _simJitterMs;
        private int _simDropPct;
        
        [SerializeField] private bool dedicatedServerMode = false;
        public static bool DedicatedServerModeActive { get; private set; }
        
    
        [SerializeField] private UnityTransport localTransport;

        [SerializeField] private NetworkManager daManager;
        [SerializeField] private UnityTransport daTransport;
        [SerializeField] private GameObject daManagerPrefab;
        [SerializeField] private GameObject localManagerPrefab;

        [Header("Game Coordinator Prefab (assign)")] [SerializeField]
        private GameObject gameCoordinatorPrefab;

        private GameObject _spawnedCoordinator;

        [Header("Local (non-DA)")] [Tooltip("Address the host listens on (0.0.0.0 on a server, 127.0.0.1 locally).")]
        public string hostListenAddress = "127.0.0.1";

        [Tooltip("Address clients connect to (server IP or localhost).")]
        public string clientConnectAddress = "127.0.0.1";

        public ushort localPort = 7777;

        [Header("DA/Relay Session")]
        [Tooltip("For Relay: host shows a join code; clients paste it here before pressing DA Client.")]
        public string daSessionName = "test-session"; // for Relay clients: set this to join code

        public int daCapacity = 3;

        [Header("UI / Auto-start")] [SerializeField]
        private GameObject mainMenuPanel; // assign "Server Menu" or parent panel here

        [Header("Standalone Client Auto-Connect")]
        [SerializeField]
        private bool autoConnectStandaloneClient = true;

        [SerializeField]
        private string standaloneClientAddress =
            "16.170.122.234";

        [SerializeField]
        private ushort standaloneClientPort = 7777;

        private NetworkManager activeNM;
        private UnityTransport activeUTP;

        enum Topology
        {
            Local,
            DA
        }

        void Awake()
        {
            //Overrides for command line arguments
            var mode = GetArg(
                "-mode",
                GetArg("mode", null)
            );
            if (!string.IsNullOrEmpty(mode))
            {
                mode = mode.ToLowerInvariant();
                _autoMode = mode switch
                {
                    "server" => AutoMode.Server,
                    "client" => AutoMode.Client,
                    "host" => AutoMode.Host,
                    _ => AutoMode.None
                };
            }
            
            //Addresses for CLI
            string explicitConnectAddress =
                GetArg("-connect", null);

            string explicitPort =
                GetArg("-port", null);

            hostListenAddress =
                GetArg("-listen", hostListenAddress);

            if (!string.IsNullOrWhiteSpace(
                    explicitConnectAddress))
            {
                clientConnectAddress =
                    explicitConnectAddress;
            }

            if (ushort.TryParse(
                    explicitPort,
                    out ushort parsedPort))
            {
                localPort = parsedPort;
            }
            
            //bot CLI
            _botMode = GetArgBool("-bot", false);
            _botMinMs = GetArgInt("-botMin", _botMinMs);
            _botMaxMs = GetArgInt("-botMax", _botMaxMs);
            
            _botExitOnDisconnect =
                GetArgBool(
                    "-exitOnDisconnect",
                    false
                );
            
            //sim CLI
            _simDelayMs = GetArgInt("-simDelayMs", 0);
            _simJitterMs = GetArgInt("-simJitterMs", 0);
            _simDropPct = GetArgInt("-simDropPct", 0);
            
            _targetFps = GetArgInt(
                "-targetFps",
                0
            );

            if (_targetFps > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate =
                    _targetFps;

                Debug.Log(
                    "[BOOT] Target frame rate capped | " +
                    $"fps={_targetFps}"
                );
            }
            
            if (_autoMode == AutoMode.Server) dedicatedServerMode = true;
            
            /*
             * A normal standalone player build should connect
             * immediately when launched by double-click.
             *
             * Explicit CLI mode/address arguments retain priority:
             * - dedicated server uses -mode=server
             * - deployment bot uses -mode=client
             *   and -connect=127.0.0.1
             */
            if (_autoMode == AutoMode.None &&
                !Application.isEditor &&
                !dedicatedServerMode &&
                autoConnectStandaloneClient)
            {
                _autoMode = AutoMode.Client;

                if (string.IsNullOrWhiteSpace(
                        explicitConnectAddress))
                {
                    clientConnectAddress =
                        standaloneClientAddress;
                }

                if (string.IsNullOrWhiteSpace(
                        explicitPort))
                {
                    localPort =
                        standaloneClientPort;
                }

                Debug.Log(
                    "[BOOT] Standalone client auto-connect enabled | " +
                    $"connect={clientConnectAddress}:{localPort}"
                );
            }
            
            //sets the instance's mode before anything else uses it
            DedicatedServerModeActive = dedicatedServerMode;
            
            // Ensure only one is active at boot (choose local by default)
            SetActiveManager(Topology.Local, activateOnly: false);
        }

        private void Start()
        {
            if (dedicatedServerMode)
            {
                
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(false);
                }
                else
                {
                    // Fallback if you didn’t wire mainMenuPanel in the Inspector
                    var serverMenu = GameObject.Find("Server Menu");
                    if (serverMenu != null)
                        serverMenu.SetActive(false);
                }
                
                SetActiveManager(Topology.Local);
                RegisterCoordinatorPrefab(activeNM);

                activeUTP.SetConnectionData(
                    clientConnectAddress,
                    localPort,
                    hostListenAddress
                );

                Debug.Log(
                    "[BOOT] Dedicated server transport configured | " +
                    $"remote={clientConnectAddress}:{localPort} | " +
                    $"listen={hostListenAddress}:{localPort}"
                );

                ApplyDebugSimIfAny(activeUTP);

                // Subscribe BEFORE StartServer so we don't miss the event
                activeNM.OnServerStarted -= OnServerStarted; // avoid duplicates
                activeNM.OnServerStarted += OnServerStarted;

                bool ok = activeNM.StartServer();
                if (!ok)
                {
                    Debug.LogError("[BOOT] Failed to start dedicated server.");
                    activeNM.OnServerStarted -= OnServerStarted;
                    return;
                }

                Debug.Log("[BOOT] Dedicated server starting (waiting for OnServerStarted to spawn coordinator)");
                return;
            }
            
            // CLI auto-start has priority
            if (_autoMode != AutoMode.None)
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

                switch (_autoMode)
                {
                    case AutoMode.Server:
                        // uses your existing dedicatedServerMode path already,
                        // but if you want it explicit:
                        // (do nothing here; dedicatedServerMode block already ran)
                        break;

                    case AutoMode.Host:
                        StartLocalHost();
                        break;

                    case AutoMode.Client:
                        StartLocalClient();
                        break;
                }

                // expose bot settings globally (client will use them)
                BotFlags.IsBot = _botMode;
                BotFlags.BotMinMs = _botMinMs;
                BotFlags.BotMaxMs = _botMaxMs;
                return;
            }
        }

        // ---------- PUBLIC BUTTONS ----------
        public void StartLocalHost()
        {
            SetActiveManager(Topology.Local);
            RegisterCoordinatorPrefab(activeNM);

            activeUTP.SetConnectionData(hostListenAddress, localPort);
            ApplyDebugSimIfAny(activeUTP);

            // Subscribe BEFORE StartHost so we don't miss the event
            activeNM.OnServerStarted -= OnServerStarted; // avoid double-subscribe
            activeNM.OnServerStarted += OnServerStarted;

            bool success = activeNM.StartHost();
            if (!success)
            {
                Debug.LogError("[BOOT] Failed to start host. Aborting coordinator spawn.");
                activeNM.OnServerStarted -= OnServerStarted;
                return;
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }
            else
            {
                GameObject serverMenu = GameObject.Find("Server Menu");
                if (serverMenu != null)
                {
                    serverMenu.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[BOOT] Server Menu object not found in the hierarchy.");
                }
            }

            Debug.Log("[BOOT] Local Host starting (waiting for OnServerStarted to spawn coordinator)");
        }

        
        private void OnServerStarted()
        {
            if (activeNM != null)
            {
                activeNM.OnServerStarted -= OnServerStarted;
            }

            Debug.Log("[BOOT] OnServerStarted fired, attempting to spawn GameCoordinator.");
            SpawnCoordinatorIfHost();
        }

        public void StartLocalClient()
        {
            SetActiveManager(Topology.Local);
            RegisterCoordinatorPrefab(activeNM);

            // CLIENT: connect to clientConnectAddress
            activeUTP.SetConnectionData(clientConnectAddress, localPort);
            
            Debug.Log(
                "[BOOT] Client transport configured | " +
                $"connect={clientConnectAddress}:{localPort}"
            );
            
            ApplyDebugSimIfAny(activeUTP);
            
            if (_botMode &&
                _botExitOnDisconnect)
            {
                activeNM.OnClientDisconnectCallback -=
                    OnBotClientDisconnected;

                activeNM.OnClientDisconnectCallback +=
                    OnBotClientDisconnected;
            }
            
            bool started =
                activeNM.StartClient();

            if (!started)
            {
                Debug.LogError(
                    "[BOOT] Failed to start client."
                );

                return;
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }
            else
            {
                GameObject serverMenu = GameObject.Find("Server Menu");
                if (serverMenu != null)
                {
                    serverMenu.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[BOOT] Server Menu object not found in the hierarchy.");
                }
            }

            Debug.Log("[BOOT] Local Client started");
        }

        private void OnBotClientDisconnected(
            ulong clientId)
        {
            if (!_botMode ||
                !_botExitOnDisconnect ||
                _applicationIsQuitting)
            {
                return;
            }

            /*
             * The bot process is a dedicated NGO client.
             * Once its own server connection disappears,
             * it has no valid reason to remain alive.
             */
            Debug.Log(
                "[BOT LIFECYCLE] Server connection lost | " +
                $"clientId={clientId} | " +
                "exiting bot process."
            );

            _applicationIsQuitting = true;

            if (activeNM != null)
            {
                activeNM.OnClientDisconnectCallback -=
                    OnBotClientDisconnected;
            }

            Application.Quit(0);
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
            if (daManager) daManager.gameObject.SetActive(false);

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

                targetNM = localManager;
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

                targetNM = daManager;
                targetUTP = daTransport;
            }

            EnsureSingleton(targetNM);
            targetNM.gameObject.SetActive(true);

            activeNM = targetNM;
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
                    try
                    {
                        other.Shutdown();
                    }
                    catch
                    {
                        /* ignore */
                    }
                }

                Destroy(other.gameObject);
            }
        }

        private void RegisterCoordinatorPrefab(NetworkManager nm)
        {
            if (!gameCoordinatorPrefab) throw new Exception("[BOOT] GameCoordinator prefab not assigned.");
            foreach (var p in nm.NetworkConfig.Prefabs.Prefabs)
                if (p.Prefab == gameCoordinatorPrefab)
                    return;

            nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = gameCoordinatorPrefab });
        }

        private void SpawnCoordinatorIfHost()
        {
            if (activeNM == null)
            {
                Debug.LogWarning("[BOOT] SpawnCoordinatorIfHost aborted: activeNM is null");
                return;
            }

            if (NetworkManager.Singleton == null ||
                NetworkManager.Singleton != activeNM ||
                !NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[BOOT] SpawnCoordinatorIfHost aborted: singleton mismatch or not server. " +
                                 $"Singleton={NetworkManager.Singleton}, activeNM={activeNM}, " +
                                 $"IsServer={NetworkManager.Singleton?.IsServer}");
                return;
            }

            if (_spawnedCoordinator != null)
            {
                Debug.Log("[BOOT] SpawnCoordinatorIfHost: coordinator already spawned, skipping.");
                return;
            }

            if (gameCoordinatorPrefab == null)
            {
                Debug.LogError("[BOOT] GameCoordinator prefab not assigned in NetBootstrap.");
                return;
            }

            _spawnedCoordinator = Instantiate(gameCoordinatorPrefab);
            var no = _spawnedCoordinator.GetComponent<NetworkObject>();
            if (no == null)
            {
                Debug.LogError("[BOOT] GameCoordinator prefab has no NetworkObject component.");
                return;
            }

            no.Spawn();
            Debug.Log("[BOOT] GameCoordinator spawned and NetworkObject.Spawn() called.");
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
                    (isHost
                        ? await DAHelpers.CreateOrGetSessionAsync(daSessionName, daCapacity)
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
                else activeNM.StartClient();

                SpawnCoordinatorIfHost();
                Debug.Log(
                    $"[BOOT][DA] Started as {(isHost ? "Host" : "Client")} via {(IsRelayEnabled ? "Relay" : "DA")}.");
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

            // For fallback, respect host/client addresses
            if (isHost)
            {
                activeUTP.SetConnectionData(hostListenAddress, localPort);
                activeNM.StartHost();
            }
            else
            {
                activeUTP.SetConnectionData(clientConnectAddress, localPort);
                activeNM.StartClient();
            }

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
                        _hostAlloc.ConnectionData, // hostConnectionData for host
                        true, // secure (DTLS)
                        true // isRelay (older API overload)
                    );

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
                        true, // secure (DTLS)
                        true
                    );

                    utp.SetRelayServerData(data);
                    return;
                }

                throw new Exception(
                    "[RELAY] No allocation found. Call CreateOrGetSessionAsync/JoinSessionAsync first.");
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
        private static string GetArg(string key, string fallback = null)
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var a in args)
            {
                if (a.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase)) 
                    return a.Substring(key.Length + 1) ;
            }

            return fallback;
        }

        private static int GetArgInt(string key, int fallback)
        {
            var s = GetArg(key, null);
            return (s != null && int.TryParse(s, out var v)) ? v : fallback;
        }

        private static bool GetArgBool(string key, bool fallback = false)
        {
            var s = GetArg(key, null);
            if (s == null) return fallback;
            return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyDebugSimIfAny(UnityTransport utp)
        {
            Debug.Log($"[BOOT][NETSIM] parsed delay={_simDelayMs} jitter={_simJitterMs} drop={_simDropPct} dev={Debug.isDebugBuild}");
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
   
#endif
        }

        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;

            if (activeNM != null)
            {
                activeNM.OnClientDisconnectCallback -=
                    OnBotClientDisconnected;
            }
        }

        private void OnDestroy()
        {
            if (activeNM != null)
            {
                activeNM.OnClientDisconnectCallback -=
                    OnBotClientDisconnected;
            }
        }
        
        public static class BotFlags
        {
            public static bool IsBot;
            public static int BotMinMs = 100;
            public static int BotMaxMs = 400;
        }
    }
}