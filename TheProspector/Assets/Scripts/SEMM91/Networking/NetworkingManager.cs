using UnityEngine;
using Unity.Netcode;

using SEMM91.Core;
namespace SEMM91.Networking
{
    public class NetworkingManager : NetworkBehaviour
    {
        public static NetworkingManager Instance { get; private set; } // Expose instance via singleton
        [SerializeField] private GameManager gameManager; //this is going to change
        private int _count; //number of participants connected, including the host
        private const int MaxClients = 2; //number of clients allowed to connect before starting game

        private int receivedClientInfluence = -1; //space reserved for client's influence value when it is delivered'
        
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
                
                GameEvents.TriggerGameIntializer();
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
                _count++;

                Debug.Log($"Client connected with ID: {clientId}. Total: {_count}");

                if (_count == MaxClients)
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
                _count--;

                Debug.Log($"Client disconnected with ID: {clientId}. Total: {_count}");
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void SendInfluenceToHostServerRpc(int influenceValue, ulong clientId)
        {
            if (IsServer)
            {
                Debug.Log($"Received influence value {influenceValue} from Client {clientId}");
                GameEvents.TriggerInfluenceTransfer(influenceValue, clientId);
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
            if (!IsServer)
            {
                Debug.Log("Client is sending influence...");
                SendInfluenceToHostServerRpc(gameManager.Influence, NetworkManager.Singleton.LocalClientId);
                gameManager.Influence = 0; //reset influence of the local client
            }
        }
        private void SetupParticipants()
        {
            // This is where you can set up participants (e.g., using RPCs)
            Debug.Log("Setting up participants...");
            gameManager.SetupParticipants();
        }
        
        
        private new void OnDestroy()
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

    }
}
