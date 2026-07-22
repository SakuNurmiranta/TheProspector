using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SEMM91.Networking
{
    public class NetBootstrap : MonoBehaviour
    {
        [Header("Managers in Scene (assign in Inspector)")]
        [SerializeField] private NetworkManager localManager;

        [SerializeField] private UnityTransport localTransport;
        [SerializeField] private GameObject localManagerPrefab;

        [Header("Game Coordinator Prefab (assign)")]
        [SerializeField] private GameObject gameCoordinatorPrefab;

        [Header("Local UTP")]
        [Tooltip("Address the host/server listens on. Use 0.0.0.0 on the dedicated server.")]
        public string hostListenAddress = "127.0.0.1";

        [Tooltip("Address clients connect to. Use server IP or localhost.")]
        public string clientConnectAddress = "127.0.0.1";

        public ushort localPort = 7777;

        [Header("UI / Auto-start")]
        [SerializeField] private GameObject mainMenuPanel;

        [Header("Standalone Client Auto-Connect")]
        [SerializeField] private bool autoConnectStandaloneClient = true;

        [SerializeField] private string standaloneClientAddress = "16.170.122.234";
        [SerializeField] private ushort standaloneClientPort = 7777;

        [Header("Dedicated Server")]
        [SerializeField] private bool dedicatedServerMode = false;

        public static bool DedicatedServerModeActive { get; private set; }

        public static bool LocalSinglePlayerModeActive
        {
            get;
            private set;
        }
        
        private enum AutoMode
        {
            None,
            Server,
            Client,
            Host
        }

        private AutoMode _autoMode = AutoMode.None;

        private bool _botMode;
        private bool _botExitOnDisconnect;
        private bool _applicationIsQuitting;

        private int _botMinMs = 100;
        private int _botMaxMs = 400;

        private int _simDelayMs;
        private int _simJitterMs;
        private int _simDropPct;

        private int _targetFps;

        private NetworkManager activeNM;
        private UnityTransport activeUTP;

        private GameObject _spawnedCoordinator;

        private ulong _connectedLocalClientId =
            ulong.MaxValue;

        public bool IsLocalClientConnected {
            get;
            private set;
        }

        public event Action LocalClientConnected;
        public event Action LocalClientDisconnected;
        
        private void Awake()
        {
            LocalSinglePlayerModeActive = false;
            
            ParseCommandLineArguments();
            ConfigureStandaloneAutoConnectIfNeeded();

            DedicatedServerModeActive = dedicatedServerMode;

            EnsureLocalManager();

            RegisterLocalClientLifecycle();

            Debug.Log(
                "[BOOT] NetBootstrap initialized | " +
                $"mode={_autoMode} | " +
                $"dedicatedServer={dedicatedServerMode} | " +
                $"bot={_botMode}"
            );
        }

        private void Start()
        {
            if (dedicatedServerMode)
            {
                StartDedicatedServer();
                return;
            }

            if (_autoMode == AutoMode.None)
                return;
            

            switch (_autoMode)
            {
                case AutoMode.Server:
                    // Server mode is handled through dedicatedServerMode above.
                    break;

                case AutoMode.Host:
                    StartLocalHost();
                    break;

                case AutoMode.Client:
                    StartLocalClient();
                    break;

                case AutoMode.None:
                default:
                    break;
            }

            PublishBotFlags();
        }

        // ---------- PUBLIC BUTTONS ----------

        public void StartLocalHost()
        {
            LocalSinglePlayerModeActive = false;

            StartLocalHostInternal();
        }

        public void StartLocalSinglePlayerHost()
        {
            LocalSinglePlayerModeActive = true;

            StartLocalHostInternal();
        }

        private void StartLocalHostInternal()
        {
            EnsureLocalManager();
            RegisterCoordinatorPrefab(activeNM);

            activeUTP.SetConnectionData(
                hostListenAddress,
                localPort
            );

            ApplyDebugSimIfAny(activeUTP);

            activeNM.OnServerStarted -=
                OnServerStarted;

            activeNM.OnServerStarted +=
                OnServerStarted;

            bool success =
                activeNM.StartHost();

            if (!success)
            {
                Debug.LogError(
                    "[BOOT] Failed to start local host."
                );

                activeNM.OnServerStarted -=
                    OnServerStarted;

                LocalSinglePlayerModeActive =
                    false;

                return;
            }

            Debug.Log(
                "[BOOT] Local host starting | " +
                $"listen={hostListenAddress}:{localPort} | " +
                $"singlePlayer=" +
                $"{LocalSinglePlayerModeActive}"
            );
        }
        public void StartLocalClient()
        {
            LocalSinglePlayerModeActive = false;
            
            EnsureLocalManager();
            RegisterCoordinatorPrefab(activeNM);

            activeUTP.SetConnectionData(clientConnectAddress, localPort);

            Debug.Log(
                "[BOOT] Client transport configured | " +
                $"connect={clientConnectAddress}:{localPort}"
            );

            ApplyDebugSimIfAny(activeUTP);
            RegisterBotDisconnectLifecycleIfNeeded();

            bool started = activeNM.StartClient();
            if (!started)
            {
                Debug.LogError("[BOOT] Failed to start client.");
                return;
            }

            Debug.Log("[BOOT] Local client started.");
        }

        // ---------- BOOT PATHS ----------

        private void StartDedicatedServer()
        {
            LocalSinglePlayerModeActive = false;
            
            HideMainMenu();

            EnsureLocalManager();
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

            activeNM.OnServerStarted -= OnServerStarted;
            activeNM.OnServerStarted += OnServerStarted;

            bool ok = activeNM.StartServer();
            if (!ok)
            {
                Debug.LogError("[BOOT] Failed to start dedicated server.");
                activeNM.OnServerStarted -= OnServerStarted;
                return;
            }

            Debug.Log(
                "[BOOT] Dedicated server starting " +
                "(waiting for OnServerStarted to spawn coordinator)."
            );
        }

        private void ParseCommandLineArguments()
        {
            string mode = GetArg(
                "-mode",
                GetArg("mode", null)
            );

            if (!string.IsNullOrWhiteSpace(mode))
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

            string explicitConnectAddress = GetArg("-connect", null);
            string explicitPort = GetArg("-port", null);

            hostListenAddress = GetArg("-listen", hostListenAddress);

            if (!string.IsNullOrWhiteSpace(explicitConnectAddress))
                clientConnectAddress = explicitConnectAddress;

            if (ushort.TryParse(explicitPort, out ushort parsedPort))
                localPort = parsedPort;

            _botMode = GetArgBool("-bot", false);
            _botMinMs = GetArgInt("-botMin", _botMinMs);
            _botMaxMs = GetArgInt("-botMax", _botMaxMs);

            _botExitOnDisconnect = GetArgBool(
                "-exitOnDisconnect",
                false
            );

            _simDelayMs = GetArgInt("-simDelayMs", 0);
            _simJitterMs = GetArgInt("-simJitterMs", 0);
            _simDropPct = GetArgInt("-simDropPct", 0);

            _targetFps = GetArgInt("-targetFps", 0);

            if (_targetFps > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = _targetFps;

                Debug.Log(
                    "[BOOT] Target frame rate capped | " +
                    $"fps={_targetFps}"
                );
            }

            if (_autoMode == AutoMode.Server)
                dedicatedServerMode = true;
        }

        private void ConfigureStandaloneAutoConnectIfNeeded()
        {
            if (_autoMode != AutoMode.None)
                return;

            if (Application.isEditor)
                return;

            if (dedicatedServerMode)
                return;

            if (!autoConnectStandaloneClient)
                return;

            _autoMode = AutoMode.Client;

            clientConnectAddress = standaloneClientAddress;
            localPort = standaloneClientPort;

            Debug.Log(
                "[BOOT] Standalone client auto-connect enabled | " +
                $"connect={clientConnectAddress}:{localPort}"
            );
        }

        // ---------- NETWORK MANAGER ----------

        private void EnsureLocalManager()
        {
            if (localManager == null && localManagerPrefab != null)
            {
                GameObject go = Instantiate(localManagerPrefab);
                go.name = "NetworkManager_Local (Runtime)";

                localManager = go.GetComponent<NetworkManager>();
            }

            if (localManager != null && localTransport == null)
            {
                localTransport =
                    localManager.GetComponent<UnityTransport>() ??
                    localManager.GetComponentInChildren<UnityTransport>(true);
            }

            if (localManager == null || localTransport == null)
            {
                throw new Exception(
                    "[BOOT] Local NetworkManager / UnityTransport not assigned " +
                    "and no valid localManagerPrefab is available."
                );
            }

            EnsureSingleton(localManager);

            if (!localManager.gameObject.activeSelf)
                localManager.gameObject.SetActive(true);

            activeNM = localManager;
            activeUTP = localTransport;
        }

        private void EnsureSingleton(NetworkManager desired)
        {
            if (NetworkManager.Singleton == null ||
                NetworkManager.Singleton == desired)
            {
                return;
            }

            NetworkManager other = NetworkManager.Singleton;

            if (other.IsListening)
            {
                try
                {
                    other.Shutdown();
                }
                catch
                {
                    // Ignore shutdown errors during bootstrap cleanup.
                }
            }

            Destroy(other.gameObject);
        }

        private void RegisterCoordinatorPrefab(NetworkManager nm)
        {
            if (gameCoordinatorPrefab == null)
                throw new Exception("[BOOT] GameCoordinator prefab not assigned.");

            foreach (NetworkPrefab prefab in nm.NetworkConfig.Prefabs.Prefabs)
            {
                if (prefab.Prefab == gameCoordinatorPrefab)
                    return;
            }

            nm.NetworkConfig.Prefabs.Add(
                new NetworkPrefab
                {
                    Prefab = gameCoordinatorPrefab
                }
            );
        }

        private void OnServerStarted()
        {
            if (activeNM != null)
                activeNM.OnServerStarted -= OnServerStarted;

            Debug.Log(
                "[BOOT] OnServerStarted fired, attempting to spawn GameCoordinator."
            );

            SpawnCoordinatorIfHost();
        }

        private void SpawnCoordinatorIfHost()
        {
            if (activeNM == null)
            {
                Debug.LogWarning(
                    "[BOOT] SpawnCoordinatorIfHost aborted: activeNM is null."
                );
                return;
            }

            if (NetworkManager.Singleton == null ||
                NetworkManager.Singleton != activeNM ||
                !NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning(
                    "[BOOT] SpawnCoordinatorIfHost aborted: singleton mismatch or not server. " +
                    $"Singleton={NetworkManager.Singleton}, " +
                    $"activeNM={activeNM}, " +
                    $"IsServer={NetworkManager.Singleton?.IsServer}"
                );
                return;
            }

            if (_spawnedCoordinator != null)
            {
                Debug.Log(
                    "[BOOT] SpawnCoordinatorIfHost: coordinator already spawned, skipping."
                );
                return;
            }

            if (gameCoordinatorPrefab == null)
            {
                Debug.LogError(
                    "[BOOT] GameCoordinator prefab not assigned in NetBootstrap."
                );
                return;
            }

            _spawnedCoordinator = Instantiate(gameCoordinatorPrefab);

            NetworkObject networkObject =
                _spawnedCoordinator.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError(
                    "[BOOT] GameCoordinator prefab has no NetworkObject component."
                );
                return;
            }

            networkObject.Spawn();

            Debug.Log(
                "[BOOT] GameCoordinator spawned and NetworkObject.Spawn() called."
            );
        }

        // ---------- BOT LIFECYCLE ----------

        private void PublishBotFlags()
        {
            BotFlags.IsBot = _botMode;
            BotFlags.BotMinMs = _botMinMs;
            BotFlags.BotMaxMs = _botMaxMs;
        }

        private void RegisterBotDisconnectLifecycleIfNeeded()
        {
            if (!_botMode || !_botExitOnDisconnect)
                return;

            activeNM.OnClientDisconnectCallback -= OnBotClientDisconnected;
            activeNM.OnClientDisconnectCallback += OnBotClientDisconnected;
        }

        private void OnBotClientDisconnected(ulong clientId)
        {
            if (!_botMode ||
                !_botExitOnDisconnect ||
                _applicationIsQuitting)
            {
                return;
            }

            Debug.Log(
                "[BOT LIFECYCLE] Server connection lost | " +
                $"clientId={clientId} | " +
                "exiting bot process."
            );

            _applicationIsQuitting = true;

            if (activeNM != null)
                activeNM.OnClientDisconnectCallback -= OnBotClientDisconnected;

            Application.Quit(0);
        }

        private void RegisterLocalClientLifecycle()
        {
            if (activeNM == null)
                return;

            activeNM.OnClientConnectedCallback -=
                HandleClientConnected;

            activeNM.OnClientConnectedCallback +=
                HandleClientConnected;

            activeNM.OnClientDisconnectCallback -=
                HandleClientDisconnected;

            activeNM.OnClientDisconnectCallback +=
                HandleClientDisconnected;
        }
        
        private void HandleClientConnected(
            ulong clientId)
        {
            if (activeNM == null ||
                !activeNM.IsClient ||
                clientId != activeNM.LocalClientId)
            {
                return;
            }

            _connectedLocalClientId =
                clientId;

            IsLocalClientConnected =
                true;

            HideMainMenu();

            Debug.Log(
                "[BOOT] Local client connection confirmed | " +
                $"clientId={clientId}"
            );

            LocalClientConnected?.Invoke();
        }

        private void HandleClientDisconnected(
            ulong clientId)
        {
            if (clientId !=
                _connectedLocalClientId)
            {
                return;
            }

            _connectedLocalClientId =
                ulong.MaxValue;

            IsLocalClientConnected =
                false;

            ShowMainMenu();

            Debug.Log(
                "[BOOT] Local client disconnected | " +
                $"clientId={clientId}"
            );

            LocalClientDisconnected?.Invoke();
        }
        
        
        // ---------- UI ----------

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                return;
            }

            Debug.LogWarning(
                "[BOOT] Cannot show the main menu because " +
                "Main Menu Panel is not assigned."
            );
        }
        
        private void HideMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
                return;
            }

            GameObject serverMenu = GameObject.Find("Server Menu");
            if (serverMenu != null)
            {
                serverMenu.SetActive(false);
                return;
            }

            Debug.LogWarning(
                "[BOOT] Main menu panel not assigned and 'Server Menu' was not found."
            );
        }

        // ---------- CLI HELPERS ----------

        private static string GetArg(string key, string fallback = null)
        {
            string[] args = Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                if (arg.StartsWith(
                        key + "=",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return arg.Substring(key.Length + 1);
                }
            }

            return fallback;
        }

        private static int GetArgInt(string key, int fallback)
        {
            string value = GetArg(key, null);

            return value != null &&
                   int.TryParse(value, out int parsed)
                ? parsed
                : fallback;
        }

        private static bool GetArgBool(string key, bool fallback = false)
        {
            string value = GetArg(key, null);

            if (value == null)
                return fallback;

            return value == "1" ||
                   value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyDebugSimIfAny(UnityTransport utp)
        {
            Debug.Log(
                "[BOOT][NETSIM] parsed " +
                $"delay={_simDelayMs} " +
                $"jitter={_simJitterMs} " +
                $"drop={_simDropPct} " +
                $"dev={Debug.isDebugBuild}"
            );

            // Placeholder retained for future Unity Transport simulator wiring.
            // No simulation is applied here yet.
        }

        // ---------- LIFECYCLE ----------

        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;

            if (activeNM != null)
                activeNM.OnClientDisconnectCallback -= 
                OnBotClientDisconnected;
                activeNM.OnClientConnectedCallback -=
                HandleClientConnected;
                activeNM.OnClientDisconnectCallback -=
                HandleClientDisconnected;
        }

        private void OnDestroy()
        {
            if (activeNM != null)
                activeNM.OnClientDisconnectCallback -= 
                OnBotClientDisconnected;
                activeNM.OnClientConnectedCallback -=
                HandleClientConnected;
                activeNM.OnClientDisconnectCallback -=
                HandleClientDisconnected;
        }

        public static class BotFlags
        {
            public static bool IsBot;
            public static int BotMinMs = 100;
            public static int BotMaxMs = 400;
        }
    }
}