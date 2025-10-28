using Unity.Netcode;
using UnityEngine;

namespace SEMM91
{
    public class GameStateMonolith : NetworkBehaviour
    {
        private void Start()
        {
            Debug.Log("Hello World!");
        }
        
        private void OnGUI()
        {
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Host"))
                {
                    NetworkManager.Singleton.StartHost();
                }
                if (GUILayout.Button("Client"))
                {
                    NetworkManager.Singleton.StartClient();
                }
                if (GUILayout.Button("Server"))
                {
                    NetworkManager.Singleton.StartServer();
                }
            }
        }
        
    }
}