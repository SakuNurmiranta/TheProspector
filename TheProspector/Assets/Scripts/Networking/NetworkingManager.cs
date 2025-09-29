using UnityEngine;
using Unity.Netcode;

namespace Networking
{
    public class NetworkingManager : NetworkBehaviour
    {
        [SerializeField] private GameManager gameManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                Debug.Log("I am the Host");
                gameManager.InitializeGame();
            }
            else
            {
                Debug.Log("I am a Client");
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
