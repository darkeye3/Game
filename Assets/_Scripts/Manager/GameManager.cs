using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    private int turn;
    public int Turn
    {
        get { return turn; }
        private set
        {
            if (turn != value)
            {
                turn = value;
                OnTurnChanged(); // Notify subscribers
            }
        }
    }

    // Declare the delegate type for turn change events
    public delegate void TurnChangeDelegate(int newTurn);

    // Declare the event using the delegate
    public event TurnChangeDelegate TurnChanged;

    private void Awake()
    {
        Turn = 0; // Initializing turn at the start.
    }

    public void IncrementTurn()
    {
        Turn += 1;
    }

    private void OnTurnChanged()
    {
        // Invoke the turn changed event
        TurnChanged?.Invoke(turn);
    }
}
