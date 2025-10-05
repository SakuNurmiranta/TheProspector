using UnityEngine;

namespace SEMM91.InputSystems
{
    public class GameControls : MonoBehaviour
    {
        private SEMM91.Core.GameManager _gameManager;

        private void Awake()
        {
            // Find the GameManager in the scene
            _gameManager = FindFirstObjectByType<SEMM91.Core.GameManager>();

            // Ensure GameManager is available
            if (_gameManager == null)
            {
                Debug.LogError("GameManager not found in the scene!");
            }
        }

        private void Update()
        {
            // Listen for SPACE key press
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_gameManager != null)
                {
                    // Route the action through GameManager
                    _gameManager.UpdateTimeFromControls();
                }
                else
                {
                    Debug.LogError("GameManager reference not set in GameControls");
                }
            }


        }
    }
}