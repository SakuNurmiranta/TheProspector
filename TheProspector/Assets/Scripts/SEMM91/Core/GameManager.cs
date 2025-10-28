using SEMM91.Core.Time;
using SEMM91.Networking;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        private int _influence;

        public bool waitForOthersTurns = false;

        public int Influence
        {
            get => _influence;
            set
            {
                _influence = value;
                Debug.Log("Influence set to " + _influence);
            }
        }
        private void Awake()
        {
            // Handle singleton logic
            if (Instance != null && Instance != this)
            {
                Debug.LogError("GameManager instance already exists! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Prevent destruction on scene load
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void InitializeGame()
        {
            Debug.Log("GameManager InitializeGame");
            
            // Delegate time initialization logic to TimeManager
            if (TimeManagerOLD.Instance != null)
            {
                TimeManagerOLD.Instance.InitializeTime();
            }
            else
            {
                Debug.LogError("TimeManager not found!");
            }
        }
        
        // This method centralizes the TimeManager update logic
        public void UpdateTimeFromControls()
        {
            if (!waitForOthersTurns)
            {
                if (TimeManagerOLD.Instance != null)
                {
                    TimeManagerOLD.Instance.UpdateTime();
                }
                else
                {
                    Debug.LogError("TimeManager not found! Cannot update time.");
                }
            }
            else
            {
                Debug.Log("Waiting for other players to finish their turn...");
            }
        }

        public void SetupParticipants()
        {
            Debug.Log("GameManager SetupParticipants");
        }

        public void UpdateInfluence(int value)
        {
            Influence += value;
            Debug.Log($"Influence updated by {value}. New total: {Influence}");
        }

        public void DeclareVictor(int clientInfluence)
        {
            if (NetworkManager.Singleton.IsHost) // Ensure this is running on the host
            {
                if (Influence > clientInfluence)
                {
                    Debug.Log("Host is the victor!");
                }
                else if (Influence < clientInfluence)
                {
                    Debug.Log("Client is the victor!");
                }
                else
                {
                    Debug.Log("It's a tie!");
                }
            }
            else
            {
                Debug.LogError("DeclareVictor() can only be called on the host.");
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameInitialized += InitializeGame;
            GameEvents.OnInfluenceTransfer += HandleInfluenceTransfer;
            GameEvents.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            GameEvents.OnGameInitialized -= InitializeGame;
            GameEvents.OnInfluenceTransfer -= HandleInfluenceTransfer;
            GameEvents.OnGameOver -= HandleGameOver;
        }
        
        private void HandleInfluenceTransfer(int influenceValue, ulong clientId)
        {
            Debug.Log($"Received influence value {influenceValue} from Client {clientId}");
            Influence = influenceValue;
        }
        
        private void HandleGameOver(string resultMessage)
        {
            Debug.Log(resultMessage);
        }
        
        public void SetWaitForOthers(bool waiting)
        {
            waitForOthersTurns = waiting;
            Debug.Log($"Setting waitForOthersTurns to {waiting}");

            if (NetworkingManager.Instance != null && NetworkManager.Singleton != null)
            {
                // Report the player's state to the server
                NetworkingManager.Instance.ReportReadyStateServerRpc(waiting, NetworkManager.Singleton.LocalClientId);
            }
        }
    }
}