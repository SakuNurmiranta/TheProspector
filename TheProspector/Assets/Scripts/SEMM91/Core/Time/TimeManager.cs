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
            
            Debug.Log($"Turn {_currentTurn} Round {_currentRound}");
            
        }
        
        //a method that tracks where agents are at any given time, so other systems can call to see if an action involving them is valid
    }

}