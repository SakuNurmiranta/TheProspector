using System;
using SEMM91.Core.Time;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        private int influence = 0;

        public int Influence
        {
            get => influence;
            set
            {
                influence = value;
                Debug.Log("Influence set to " + influence);
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
    }
}