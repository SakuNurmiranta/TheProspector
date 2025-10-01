using UnityEngine;
using Unity.Netcode;

using SEMM91.Core;
namespace SEMM91.Networking
{
    public class NetworkingManager : NetworkBehaviour
    {
        [SerializeField] private GameManager gameManager;
        private int count;
        private const int maxClients = 2;
        
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
        }
        
        void Update()
        {
            if (IsServer)
            {
                // Host logic here
            }
            else
            {
                // Client logic here
            }
        }
    }
}
