using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayModel : GameModel
{
    //0 1 => 1이닝 공격 수비, => 0~17 => 짝수면 원정, 홀수면 홈 
    private int inning = 0;
    private int out_count = 0;

    private Queue<Batter>[] runners = new Queue<Batter>[MAX_BASE_COUNT + 1];
    private TeamStatus[] _teamStatus = new TeamStatus[2];

    //Define
    public const int MAX_INNING_COUNT = 18;
    public const int MAX_OUT_COUNT = 3;
    public const int MAX_BASE_COUNT = 3;

    public GamePlayModel()
    {
        for (int i = 0; i < MAX_BASE_COUNT + 1; i++)
        {
            runners[i] = new Queue<Batter>();
        }
    }
    
    //property
    public int OutCount
    {
        get { return out_count; }
        set
        {
            out_count = value;
        }
    }
    
    public int Inning
    {
        get { return inning; }
        set
        {
            inning = value;
        }
    }

    public void AddRunnder(int index, Batter batter)
    {
        runners[index].Enqueue(batter);
    }
    public Batter GetRunner(int index)
    {
        return runners[index].Peek();
    }

    public bool IsEmptyRunner(int index)
    {
        if (runners[index].Count == 0)
        {
            return true;
        }
        return false;
    }

    public Batter RemoveRunner(int index)
    {
        return runners[index].Dequeue();
    }

    public int EstimateRunners()
    {
        int count = 0;
        for (int i = 0; i < runners.Length; i++)
        {
            count += runners[i].Count;
        }
        return count;
    }

    public int GetTeamIndex()
    {
        return inning % 2;
    }

    public int AddScore(int value)
    {
        _teamStatus[GetTeamIndex()].Score += value;
        return _teamStatus[GetTeamIndex()].Score;
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