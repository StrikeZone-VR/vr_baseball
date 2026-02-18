using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayModel : GameModel
{
    public BaseStatusPanel _baseStatusPanel; //debug

    //0 1 => 1이닝 공격 수비, => 0~17 => 짝수면 원정, 홀수면 홈 
    private int inning = 0;
    private int out_count = 0;

    //베이스에 있는 주자들 : List로 해도 하도 주자가 적어서 동적으로 지워도 된다
    private List<Batter> runners = new List<Batter>();
    private TeamStatus[] _teamStatus = new TeamStatus[2];

    //Define
    public const int MAX_INNING_COUNT = 18;
    public const int MAX_OUT_COUNT = 3;
    public const int MAX_BASE_COUNT = 4;

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

    //[v] 내가 달릴때, [x] 원래는 주자가 달릴때 , [x] 바꿔치기 할때 
    public void AddRunner(Batter batter)
    {
        Debug.Log("추가 : " + batter.BaseIndex);
        runners.Add(batter);
        DebugBaseStatus();
        
    }
    public void ReplaceLastRunner(Batter batter)
    {
        if (runners.Count == 0)
        {
            Debug.LogError("대체할 러너가 없엉");
            return;
        }
        runners[runners.Count - 1] = batter;
    }
    
    public Batter RemoveRunner(int base_index)
    {
        Debug.Log("제거 : " + base_index);
        for (int i = 0; i < runners.Count; i++)
        {
            if (runners[i].BaseIndex == base_index)
            {
                Batter batter = runners[i];
                runners.RemoveAt(i);
                return batter;
            }
        }
        Debug.LogError("제거할 runner가 없는뎁쇼?");
        return null;
    }
    
    public Batter GetRunner(int base_index)
    {
        //거꾸로 찾아야지 맨 앞 주자의 정보를 가져올 수 있다.
        for (int i = runners.Count - 1; i >= 0; i--)
        {
            if (runners[i].BaseIndex == base_index)
            {
                Batter batter = runners[i];
                return batter;
            }
        }
        return null;
    }

    public int RunningIndex()
    {
        for (int i = 0; i < runners.Count; i++)
        {
            if (runners[i].IsMove)
            {
                return runners[i].BaseIndex;
            }
        }
        return -1;
    }

    public void MoveBase()
    {
        for (int i = 0; i < runners.Count; i++)
        {
            runners[i].BaseIndex++;
        }
    }

    public bool IsEmptyRunner(int base_index)
    {
        //거꾸로 찾아야지 맨 앞 주자의 정보를 가져올 수 있다.
        for (int i = 0; i < runners.Count; i++)
        {
            if (runners[i].BaseIndex == base_index)
            {
                return false;
            }
        }
        return true;
    }

    public List<Batter> GetRunners()
    {
        return runners;
    }
    public void ClearRunner()
    {
        runners.Clear();
    }

    //러너 측정
    public int GetRunnerCount()
    {
        return runners.Count;
    }

    public Batter GetLastRunner()
    {
        return runners[runners.Count - 1];
    }
    
    public int GetRunnerIndexCount(int base_index)
    {
        int count = 0;
        for (int i = 0; i < runners.Count; i++)
        {
            if (runners[i].BaseIndex == base_index)
            {
                count++;
            }
        }
        return count;
    }

    public void RunSignal()
    {
        for (int i = 0; i < runners.Count; i++)
        {
            runners[i].IsMove = true;
        }
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


    //대충 base_index와 Runner간의 상호작용이 안된듯
    public void DebugBaseStatus()
    {
        _baseStatusPanel.SetInit();
        
        for (int i = 0; i < runners.Count; i++)
        {
            //주자
            if (runners[i].IsMove)
            {
                _baseStatusPanel.SetBaseLine(runners[i].BaseIndex, true);
            }
            else //베이스
            {
                _baseStatusPanel.SetBase(runners[i].BaseIndex, true);
            }
        }

        DebugPrintBaseStatus();
    }

    public void DebugPrintBaseStatus()
    {
        if (runners.Count == 0)
        {
            return;
        }
        if (runners[0].BaseIndex == 0 && runners[0].IsMove == false && runners.Count == 1)
        {
            return; //초반 안타 대기 
        }
        Debug.Log("베이스");
        for (int i = 0; i < runners.Count; i++)
        {
            Debug.Log(i + "base [" + runners[i].BaseIndex + "] : " + runners[i].name);

        }
        Debug.Log("-------------");
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