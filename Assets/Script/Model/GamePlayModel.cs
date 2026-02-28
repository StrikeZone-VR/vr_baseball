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
    private List<int> before_runners = new List<int>(); //타입을 int만 해도 될듯?
                                                        //홈에 들어온 사람 있다면 맨 앞에 주자 추가
                                                        //이후 모든 주자 for문으로 SetBase()와 BaseIndex = before값
    //current = mybody나 다른 타자가 왔을 때 저장하고
    //파울이나 플라잉아웃이면 그거 참조
    //파울은 돌아가고 플라잉 아웃이면 되돌아가야함
    // ㄴ 근데 플라잉아웃이면 그거를 또 생성해서 되돌아가야하냐
    
    
    
    private int before_score = 0; //만약 before_score와 score값이 같다면 => 홈에 들어온 사람이 없다.
    //만약 있다면? 되돌릴때 runners.insert(맨앞)
    //근데 이러면 또 CreateBatter에서 스트라이크 볼 초기화되는 문제가 생기는 구나

    private TeamStatus[] _teamStatus = new TeamStatus[2];
    //사실 점수만 하고 싶은데

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
            //나중에 아웃 관련된 UI에 넣어야 함
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

    public int BeforeScore
    {
        get { return before_score; }
        set { before_score = value; }
    }

    public int GetScore()
    {
        return _teamStatus[GetTeamIndex()].Score;
    }

    //[v] 내가 달릴때, [x] 원래는 주자가 달릴때 , [x] 바꿔치기 할때 
    public void AddRunner(Batter batter)
    {
        runners.Add(batter);
        DebugBaseStatus(false); //플라잉 아웃이 아닌게 확정이니까
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
    
    public void MoveBaseRunner()
    {
        for (int i = 0; i < runners.Count; i++)
        {
            runners[i].SetBaseIndex(runners[i].BaseIndex + 1);
        }
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

    public void RemoveLastRunner()
    {
        if (runners.Count <= 0)
        {
            Debug.LogError("제거할 runner가 없는뎁쇼?");
            return;
        }
        runners.RemoveAt(runners.Count - 1);
    }
    
    public Batter GetRunner(int base_index)
    {
        //거꾸로 찾아야지 맨 앞 주자의 정보를 가져올 수 있다.
        for (int i = 0; i < runners.Count; i++)
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
            //Debug.Log(i + " : " + runners[i].IsMove);
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

    public void InsertRunner(Batter batter)
    {
        runners.Insert(0, batter);
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

    //runners, score
    public void SaveBeforeStatus()
    {
        before_score = _teamStatus[GetTeamIndex()].Score;
        
        before_runners.Clear();
        for (int i = 0; i < runners.Count; i++)
        {
            before_runners.Add(runners[i].BaseIndex);
        }
    }
    
    //foul
    public void FoulRollbackBeforeStatus()
    {
        _teamStatus[GetTeamIndex()].Score = before_score;

        //Debug.Log("주자의 갯수 : " + runners.Count);
        //그리고 주자는 하나 없어야 함. => 즉, run signal 보내기 전으로 되돌려야 함 
        //그리고 주자 맨 뒤는 제거. 혹시 모르니 if문으로 사이즈 오버되면 null처리
        for (int i = 0; i < before_runners.Count; i++)
        {
            runners[i].SetBaseIndex(before_runners[i]);
            runners[i].IsMove = false;
        }
    }
    
    /// <summary>
    /// 어차피 이거 전에는 아웃 => 혹시 점수 바뀌면 전 사람 소환
    /// 그리고 타석에 선 주자는 이미 아웃이라 상관없다. 
    /// </summary>
    public void FlyingOutRollbackBeforeStatus()
    {
        _teamStatus[GetTeamIndex()].Score = before_score;

        // 그냥 before 비교하고
        // before값에서 -1로 설정하고
        // 그러고 MoveBase 지정 이런거 해야할듯
        for (int i = 0; i < before_runners.Count; i++)
        {
            //Debug.Log("[runner] : " + runners[i].name);
            runners[i].BaseIndex = before_runners[i] - 1; //되돌아가줘
            runners[i].IsMove = true;
            //runners[i].SetBaseIndex(before_runners[i]);
        }
    }

    public void DebugBeforeStatus()
    {
        Debug.Log("전 점수 : " + before_score);
        
        for (int i = 0; i < before_runners.Count; i++)
        {
            Debug.Log("[runner] : " + before_runners[i]);
        }
    }

    //대충 base_index와 Runner간의 상호작용이 안된듯
    public void DebugBaseStatus(bool isOut)
    {
        _baseStatusPanel.SetInit();
        int value = 0;
        if (isOut) value = 1; 
        for (int i = 0; i < runners.Count; i++)
        {
            //주자
            if (runners[i].IsMove)
            {
                _baseStatusPanel.SetBaseLine(runners[i].BaseIndex + value, true);
            }
            else //베이스
            {
                _baseStatusPanel.SetBase(runners[i].BaseIndex + value, true);
            }
        }

        //DebugPrintBaseStatus();
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
            Debug.Log(i + "base [" + runners[i].BaseIndex + "] : " + runners[i].name + "의 move" + runners[i].IsMove);

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