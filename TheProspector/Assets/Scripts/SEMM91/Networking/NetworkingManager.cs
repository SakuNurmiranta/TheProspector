using UnityEngine;
using Unity.Netcode;

using SEMM91.Core;
namespace SEMM91.Networking
{
    public class NetworkingManager : NetworkBehaviour
    {
        public static NetworkingManager Instance { get; private set; } // Expose instance via singleton
        [SerializeField] private GameManager gameManager;
        private int count;
        private const int maxClients = 2;

        private int receivedClientInfluence = -1;
        
        private void Awake()
        {
            // Singleton implementation
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Duplicate NetworkingManager instance found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                Debug.Log("I am the Host");
                
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                
                gameManager.InitializeGame();
            }
            else
            {
                Debug.Log("I am a Client");
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (IsServer)
            {
                count++;

                Debug.Log($"Client connected with ID: {clientId}. Total: {count}");

                if (count == maxClients)
                {
                    Debug.Log("Two clients connected. Setting up participants...");
                    SetupParticipants();
                }
            }
        }
        private void OnClientDisconnected(ulong clientId)
        {
            if (IsServer)
            {
                count--;

                Debug.Log($"Client disconnected with ID: {clientId}. Total: {count}");
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void SendInfluenceToHostServerRpc(int influenceValue, ulong clientId)
        {
            if (IsServer)
            {
                Debug.Log($"Received influence value {influenceValue} from Client {clientId}");
                receivedClientInfluence = influenceValue;

                // Compare client's influence with host's influence and declare the victor
                if (gameManager != null)
                {
                    gameManager.DeclareVictor(receivedClientInfluence);
                }
            }
        }

        [ClientRpc]
        public void NotifyGameResultClientRpc(string resultMessage)
        {
            Debug.Log(resultMessage);
        }

        // Method for clients to send their influence during important gameplay events
        public void SendInfluence()
        {
            if (!IsServer && NetworkManager.Singleton.LocalClientId != null)
            {
                Debug.Log("Client is sending influence...");
                SendInfluenceToHostServerRpc(gameManager.Influence, NetworkManager.Singleton.LocalClientId);
            }
        }
        private void SetupParticipants()
        {
            // This is where you can set up participants (e.g., using RPCs)
            Debug.Log("Setting up participants...");
            gameManager.SetupParticipants();
        }
        
        
        private void OnDestroy()
        {
            // Unsubscribe from events when the object is destroyed (e.g., on scene unload)
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            // Clear singleton reference
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        void Update()
        {
            
            
        }
    }
}
