using System;
using SEMM91.Core.Time;
using UnityEngine;

namespace SEMM91.Core
{
    public class GameManager : MonoBehaviour
    {
        private void Start()
        {
           InitializeGame();
        }

        private void Update()
        {
            
        }

        public void InitializeGame()
        {
            Debug.Log("GameManager InitializeGame");
            
            // Delegate time initialization logic to TimeManager
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.InitializeTime();
            }
            else
            {
                Debug.LogError("TimeManager not found!");
            }
        }
        
        // This method centralizes the TimeManager update logic
        public void UpdateTimeFromControls()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.UpdateTime();
            }
            else
            {
                Debug.LogError("TimeManager not found! Cannot update time.");
            }
        }

        public void SetupParticipants()
        {
            Debug.Log("GameManager SetupParticipants");
        }

    }
}