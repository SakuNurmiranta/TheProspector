using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{

    private enum PlayerRole
    {
        Keeper,
        Regular,
        Pariah
    }

    private PlayerRole? lastLogged = null;
    private int currentTurn = 0;
    private int maxTurns = 4;
    private bool isSpacePressed = false;
    void Start()
    {

        LogPlayerRole();
        InitializeGame();
        //run first host check
        //start first turn of the first round
    }

   
    void Update()
    {
        //isSpacePressed = false;
        LogPlayerRole();

        //SimulateTurn();
        
        if (IsServer)
        {
            //Debug.Log("Handling Host Logic...");
            //         wait for others
            //         update own scene
            //         create update packets for clients
            //         send update packets
            //         wait for clients to update
            //         run emigration sequence
            //         wait for emigration clearance
        }
        else
        {
            //Debug.Log("Handling Client Logic...");
            //         send bucket to host
            //         wait for update packets
            //         if asked, agree to hosting
        } 
        
    }
    
    private void LogPlayerRole()
    {
        if (IsServer)
        {
            if (lastLogged != PlayerRole.Keeper)
            {
                Debug.Log("I am the Host");
                lastLogged = PlayerRole.Keeper;
            }
        }
        else 
        {
            if (lastLogged != PlayerRole.Regular) {

                Debug.Log("I am the Client");
                lastLogged = PlayerRole.Regular;
            }
        }
    }

    private void InitializeGame()
    {
        Debug.Log("Initializing Game");
    }
    
    private void SimulateTurn()
    {
        StartCoroutine(SimulateTurnCoroutine());
    }
    
    private IEnumerator SimulateTurnCoroutine()
    {
        Debug.Log("Press spacebar to Simulate a Turn");
        while (!Input.GetKeyDown(KeyCode.Space))
        {
            yield return null;
        }
        
        Debug.Log("Simulating Turn");    // Update the turn
        currentTurn++;
        Debug.Log($"Turn {currentTurn} simulated");

        // Handle end of round logic if needed
        if (currentTurn >= maxTurns)
        {
            Debug.Log("Round completed!");
            currentTurn = 0; // Reset turn for the new round
        }
    }
    
}
