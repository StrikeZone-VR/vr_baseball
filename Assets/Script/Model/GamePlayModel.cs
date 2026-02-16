using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayModel : GameModel
{
    public BaseStatusPanel _baseStatusPanel; //debug

    //0 1 => 1이닝 공격 수비, => 0~17 => 짝수면 원정, 홀수면 홈 
    private int inning = 0;
    private int out_count = 0;

    //베이스에 있는 주자는 
    private Queue<Batter>[] runners = new Queue<Batter>[MAX_BASE_COUNT];
    private TeamStatus[] _teamStatus = new TeamStatus[2];

    //Define
    public const int MAX_INNING_COUNT = 18;
    public const int MAX_OUT_COUNT = 3;
    public const int MAX_BASE_COUNT = 4;

    public GamePlayModel()
    {
        for (int i = 0; i < MAX_BASE_COUNT; i++)
        {
            runners[i] = new Queue<Batter>();
        }
    }

    public void SetPanel(BaseStatusPanel baseStatusPanel)
    {
        this._baseStatusPanel = baseStatusPanel;
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
        _baseStatusPanel.SetBaseLine(index, batter);
    }
    
    public Batter RemoveRunner(int index)
    {
        if (IsEmptyRunner(index))
        {
            Debug.LogError("Trying to remove runner, but there is no runner");
            return null;
        }
        return runners[index].Dequeue();
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

    public int GetRunnerCount(int index)
    {
        return runners[index].Count;
    }

    public void DebugBaseStatus()
    {
        for (int i = 0; i < MAX_BASE_COUNT - 1; i++)
        {
            if (runners[i].Count != 0) // 0 1 2 3
            {
                //base line
                if (runners[i].Peek().IsMove)
                {
                    _baseStatusPanel.SetBaseLine(i, true);
                    _baseStatusPanel.SetBase(i, false);
                }
                else //base
                {
                    _baseStatusPanel.SetBase(i, true);
                    _baseStatusPanel.SetBaseLine(i, false);
                }
            }
            else //비어있다면
            {
                _baseStatusPanel.SetBase(i, false);
                _baseStatusPanel.SetBaseLine(i, false);
            }
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