using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    private int _currentTurn = 0;
    private readonly int _maxTurns = 4;

    public void InitializeGame()
    {
        Debug.Log("Initializing Game");
    }

    public void SimulateTurn()
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
        
        Debug.Log("Simulating Turn");
        _currentTurn++;
        Debug.Log($"Turn {_currentTurn} simulated");

        if (_currentTurn >= _maxTurns)
        {
            Debug.Log("Round completed!");
            _currentTurn = 0; // Reset turn for the new round
        }
    }
    
}
