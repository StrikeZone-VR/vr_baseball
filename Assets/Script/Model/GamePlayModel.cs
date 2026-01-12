using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayModel : GameModel
{
    //0 1 => 1이닝 공격 수비, => 0~17 => 짝수면 원정, 홀수면 홈 
    private int inning = 0;
    
    private Queue<Batter>[] runners = new Queue<Batter>[MAX_BASE_COUNT + 1];
    private TeamStatus[] _teamStatus = new TeamStatus[2];

    //Define
    private const int MAX_INNING_COUNT = 18;
    private const int MAX_BASE_COUNT = 3;
    
    GamePlayModel()
    {
        for (int i = 0; i < MAX_BASE_COUNT + 1; i++)
        {
            runners[i] = new Queue<Batter>();
        }
    }
}

struct TeamStatus
{
    private int score;

    //타순 0 ~ 8
    public int batting_order;

    //Define
    private const int MAX_BATTING_ORDER = 9;

    public int BattingOrder
    {
        get => batting_order;
        set
        {
            batting_order = value;
            if (batting_order >= MAX_BATTING_ORDER)
            {
                batting_order = 0;
            }
        }
    }

    /// <summary>
    ///only AddScore function
    /// </summary>
    public int Score
    {
        get => score;

        set { score = value; }
    }
}