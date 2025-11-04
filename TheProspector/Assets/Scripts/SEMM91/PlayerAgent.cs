using Unity.Netcode;
using UnityEngine;

public class PlayerAgent : NetworkBehaviour
{
    // local input count (client-side) shown just for feedback
    private int localTurn;

    private void Update()
    {
        if (!IsOwner || !IsClient) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Ask whoever is authoritative over the coordinator to count our press
            SubmitTurnServerRpc();
        }
    }

    [ServerRpc]
    private void SubmitTurnServerRpc(ServerRpcParams p = default)
    {
        var g = GameCoordinator.Instance;
        if (g == null) return;
        g.RegisterTurn(p.Receive.SenderClientId);
    }

    private void OnGUI()
    {
        if (!IsOwner) return;
        GUILayout.BeginArea(new Rect(10, 10 + 80 * (int)NetworkManager.Singleton.LocalClientId, 340, 70));
        GUILayout.Label($"Client {NetworkManager.Singleton.LocalClientId} — press SPACE to add points");
        GUILayout.EndArea();
    }
}