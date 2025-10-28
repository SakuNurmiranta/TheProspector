using Unity.Netcode;
using UnityEngine;

public class PlayerShrink2D : NetworkBehaviour
{
    private Vector3 initialScale; // To store the starting scale

    private void Start()
    {
        // Record the initial size of the sprite
        initialScale = transform.localScale;
    }

    private void Update()
    {
        // Ensure only the owner of the object can control it
        if (!IsOwner) return;

        // Detect Space key press to shrink
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShrinkPlayer();
        }
    }

    private void ShrinkPlayer()
    {
        // Prevent the sprite from becoming too small
        if (transform.localScale.x <= 0.1f || transform.localScale.y <= 0.1f)
            return;

        // Shrink the player locally
        transform.localScale *= 0.9f;

        // Synchronize the scale across all connected clients
        ShrinkServerRpc();
    }

    [ServerRpc]
    private void ShrinkServerRpc()
    {
        // Synchronize the shrinking across clients
        ShrinkClientRpc();
    }

    [ClientRpc]
    private void ShrinkClientRpc()
    {
        // Apply the shrink effect on all connected clients
        transform.localScale *= 0.9f;
    }

    private void OnDisable()
    {
        // Reset to initial scale if the object is disabled or destroyed
        transform.localScale = initialScale;
    }
}