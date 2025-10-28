using System;
using UnityEngine;

public static class GameEvents
{
    //event for init
    public static event Action OnGameInitialized;
    
    //event for influence transfer
    public static event Action<int, ulong> OnInfluenceTransfer;
    
    //event for advancing to next turn
    public static event Action OnAdvanceTurn;
    
    //event for game over
    public static event Action<string> OnGameOver;

    public static event Action OnAllPlayersReady;
    
    public static void TriggerGameIntializer() => OnGameInitialized?.Invoke();
    public static void TriggerInfluenceTransfer(int influence, ulong influenceId) => OnInfluenceTransfer?.Invoke(influence, influenceId);
    public static void TriggerGameOver(string winner) => OnGameOver?.Invoke(winner);
    
    public static void TriggerAdvanceTurn() => OnAdvanceTurn?.Invoke();
    
    public static void TriggerAllPlayersReady() => OnAllPlayersReady?.Invoke();
}
