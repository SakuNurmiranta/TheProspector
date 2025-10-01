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

    }
}