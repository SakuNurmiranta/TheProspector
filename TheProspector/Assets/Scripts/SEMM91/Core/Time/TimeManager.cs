using SEMM91.Networking;
using SEMM91.Core;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.Core.Time
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }
        
        private int _currentTurn;
        private int _currentRound;
        public int GetCurrentTurn => _currentTurn;
        public int GetCurrentRound => _currentRound;

        private void Awake()
        {
            //standard singleton pattern maneuvers
            if (Instance != null && Instance != this )
            {
                Debug.LogError("Had to destroy a duplicate TimeManager instance.");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
        }
        
        
        
        //a method to initialize time
        public void InitializeTime()
        {
            Debug.Log("Initializing Time");
            _currentTurn = 0;
            _currentRound = 0;
        }
        
        //a method that updates turns and rounds on numerical level
        public void UpdateTime()
        {
            
            _currentTurn++;
            
            if (_currentTurn > 3)
            {
                _currentRound++;
                _currentTurn = 0;
            }
            
            // Only update influence on turn progression
            
            UpdateInfluenceForTurn();
            
            Debug.Log($"Turn {_currentTurn} Round {_currentRound}");
            
        }
        
        private void UpdateInfluenceForTurn()
        {
            if (GameManager.Instance != null)
            {
                // Only server/host decides the influence increase
                int influenceIncrease;

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                {
                    // Server logic: Always gives +1
                    influenceIncrease = 1;
                }
                else
                {
                    // Client logic: Randomly adds +1 or +2
                    influenceIncrease = UnityEngine.Random.Range(1, 3);
                }

                // Apply influence
                GameManager.Instance.UpdateInfluence(influenceIncrease);
                
                // If we're a client, send our influence value to the server
                if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost)
                {
                    NetworkingManager.Instance.SendInfluence();
                }
                
            }
        }
        
        //a method that tracks where agents are at any given time, so other systems can call to see if an action involving them is valid
    }

}