using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;

//게임 시작할때 실행되는 GameManager
public class GameManager : MonoBehaviour
{
    [SerializeField] private XROrigin playerOrigin;
    
    [SerializeField] private Baseball _ball; //일단 PitchingBallController도 여깄음

    [Header("Broadcasting on EventChannels")] 
    [SerializeField] private VoidEventSO addBallCountEvent; //to Baseball
    [SerializeField] private VoidEventSO strikeEvent; // toStrikeZone, batter
    [SerializeField] private VoidEventSO foulEvent; // toStrikeZone
    [SerializeField] private VoidEventSO homerunEvent; // toStrikeZone

    [Space]
    [SerializeField] private VoidEventSO startPitchEvent; //to batter
    [SerializeField] private VoidEventSO swingEvent; //to Pitcher, auto swing
    [SerializeField] private VoidEventSO pitchEvent; //to PitchingBallController
    [SerializeField] private VoidEventSO backToPitcherEvent; //?

    [Header("Manager")]
    [SerializeField] private PitchingManager pitchingManager; //todo : 아직 안봄

    protected List<GameModel> gameModels = new List<GameModel>();
    
    private void OnEnable()
    {
        addBallCountEvent.onEventRaised += AddBallCount;
        strikeEvent.onEventRaised += AddStrike;
        foulEvent.onEventRaised += Paul;
        homerunEvent.onEventRaised += Homerun;

        startPitchEvent.onEventRaised += OnTouchBall;

        swingEvent.onEventRaised += DebugBatting;
        pitchEvent.onEventRaised += SwingSignalToBatter;
        backToPitcherEvent.onEventRaised += PitcherGetBall;
    }

    private void OnDisable()
    {
        addBallCountEvent.onEventRaised -= AddBallCount;
        strikeEvent.onEventRaised -= AddStrike;
        foulEvent.onEventRaised -= Paul;
        homerunEvent.onEventRaised -= Homerun;

        startPitchEvent.onEventRaised -= OnTouchBall;

        swingEvent.onEventRaised -= DebugBatting;
        pitchEvent.onEventRaised -= SwingSignalToBatter;
        backToPitcherEvent.onEventRaised -= PitcherGetBall;
    }

    private void Start()
    {
        SetScore(0, 0);
        SetScore(1, 0);
        Inning = 0;

        //pitcher has ball
        //_ball.RemovePlayer();
        //defenders[0].SetMyBall(_ball);
    }

    #region PROPERTY

    public int Inning
    {
        get { return inning; }
        set
        {
            if (value >= MAX_INNING_COUNT)
            {
                Debug.Log("Game Over, back to the menu...");

                //GameEnd
                return;
            }

            inning = value;
            InitInning();


            int num = inning % 2;

            //change 
            if (num == 0)
            {
                StartBatter();
            }
            else
            {
                StartPitcher();
            }

            string t = inning % 2 == 0 ? "▲" : "▼";
            t += " " + (inning / 2 + 1) + "이닝";
            _inningText.text = t;
        }
    }

    public int Strike
    {
        get { return strike_count; }
        set
        {
            //상태저장
            strike_count = value;
            
            //out
            if (strike_count >= MAX_STRIKE_COUNT)
            {
                strike_count = 0;

                DeleteRunner();
                AddOut();
            }
            else
            {
                //아니 근데 여기서 달리기를 되돌리는건 좀
            }

            //ui
            _UIGameStatusElements[1].SetIndex(strike_count);
        }
    }

    public int BallCount
    {
        get { return ball_count; }
        set
        {
            ball_count = value;
            if (ball_count >= MAX_BALL_COUNT)
            {
                ball_count = 0;
                _ball.IsBatTouch = false;

                //AddBaseStatus();
                MoveOneBase();
            }

            _UIGameStatusElements[0].SetIndex(ball_count);
        }
    }

    private void AddBallCount()
    {
        BallCount++;
    }

    private void AddIsBaseStatus(int index)
    {
        Debug.Log("나오면 안돼");
        Batter batter = runners[index].Dequeue();
        runners[index + 1].Enqueue(batter);
    }

    void AddStrike()
    {
        Strike++;
    }

    private void AddScore()
    {
        Batter batter = runners[3].Dequeue();
        Destroy(batter.gameObject);

        SetScore(inning % 2, ++_teamStatus[inning % 2].Score);
    }

    private void Paul()
    {
        PitcherGetBall();

        Debug.Log("Paul");
        //만약에 점수를 냈다면?
        RerollBeforeStatus();
        
        //strike == 2
        if (Strike == MAX_STRIKE_COUNT - 1)
        {
            return;
        }

        AddStrike();
    }

    private void Homerun()
    {
        PitcherGetBall();
        SetScore(inning % 2, ++_teamStatus[inning % 2].Score);
        for (int i = 0; i < runners.Length; i++)
        {
            _teamStatus[inning % 2].Score += runners[i].Count;
            SetScore(inning % 2, _teamStatus[inning % 2].Score);
        }

        ClearRunners();
    }


    private void SetScore(int teamIndex, int score)
    {
        _teamStatus[teamIndex].Score = score;
        _scoreTexts[teamIndex].text = (_teamStatus[teamIndex].Score).ToString();
    }

    private void InitInning()
    {
        BallCount = 0;
        Strike = 0;
        OutCount = 0;

        ClearRunners();
    }

    // ********************************************************************************************************************************************end

    #endregion

    private void RerollBeforeStatus()
    {
        Debug.Log("back to the future");

        //되돌아가자
        for (int i = 1; i < runners.Length; i++)
        {
            while (runners[i].Count > 0)
            {
                Batter batter = runners[i].Peek();
                batter.IsMove = false;
                batter.transform.position = bases[i].position;
            }
        }

        //그냥 아웃처리하자
        if (runners[0].Count == 0)
        {
            return;
        }
        Batter b = runners[0].Dequeue();
        b.IsMove = false;
        b.transform.position = batterPosition.position;
    }
    

    private bool IsCheckBeforeStatus()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            if (isBeforeBaseStatus[i] != (runners[i].Count != 0))
            {
                return true;
            }
        }

        return false;
    }

    private void DeleteRunner()
    {
        Destroy(currentBatter.gameObject);
        currentBatter = null;
        CreateBatter();
    }
    
    private void StartBatter()
    {
        Debug.Log("타자 Mode On");

        pitchingManager.EndPitchingGame();
        defenders[0].gameObject.SetActive(true);
        defenders[0].SetMyBall(_ball);

        //방망이 중력, rotation position 풀기
        playerOrigin.MoveCameraToWorldLocation(new Vector3(0, 1.0f, 0)); //시점 타자 시점
    }

    private void StartPitcher()
    {
        Debug.Log("투수 Mode On");

        defenders[0].IsTracking = false;
        pitchingManager.StartPitchingGame();
        defenders[0].gameObject.SetActive(false);

        //방망이 위치 Vector3(-0.660000026,1.37,0.150000006) 여기로
        //방망이 중력, rotation position 얼리기
        CreateBatter();

        playerOrigin.MoveCameraToWorldLocation(new Vector3(-10, 1.0f, -10));
        playerOrigin.MatchOriginUpCameraForward(Vector3.up, new Vector3(1, 0, 1));
    }

    void PitcherGetBall()
    {
        Debug.Log("공공이 돌아옴");
        pitchingManager.ResetBall();
    }


    private float GetDistanceBetween(Vector3 a, Vector3 b)
    {
        float result = Vector3.Distance(a, b);
        return result;
    }

    private void AllTrackingOff()
    {
        for (int i = 0; i < defenders.Length; i++)
        {
            if (defenders[i].gameObject.activeSelf)
                defenders[i].IsTracking = false;
        }
    }

    #region BATTER

    /// <summary> runner clear </summary>
    private void ClearRunners()
    {
        if (currentBatter)
        {
            if (currentBatter.gameObject)
            {
                Destroy(currentBatter.gameObject);
                currentBatter = null;
            }
        }
        for (int i = 0; i < runners.Length; i++)
        {
            while (runners[i].Count > 0)
            {
                Batter batter = runners[i].Dequeue();
                if(!batter)
                    Destroy(batter.gameObject);
            }
        }
    }

    private void RunRunner()
    {
        Debug.Log("이 메세지가 두번 나온다면");
        runners[0].Enqueue(currentBatter);

        currentBatter.SetBases(bases);

        currentBatter.transform.position = bases[3].position;
        currentBatter.BaseIndex = 0;
        currentBatter.IsMove = true;
    }

    private void CreateBatter() //AI
    {
        Debug.Log("타자 생성");
        Batter batter = Instantiate(batterPrefab, batterCreatePosition.position, Quaternion.identity);

        batter.SetBall(_ball);
        batter.SetBat(_bat);
        batter.transform.parent = batterCreatePosition;

        //베트 자리로 이동
        batter.MovePlayer(batterPosition.position);

        currentBatter = batter;
        _ball.OffTouchBall();

        //batter

        //runners[0].transform.rotation = Quaternion.LookRotation(bases[2].position);
    }


    private void OnTouchBall()
    {
        _ball.OnTouchBall();
    }

    private void DebugBatting()
    {
        //batter.DebugHitting();

        // float x = Random.Range(-1.0f, 0f);
        // float z = Random.Range(-1.0f, 0f);
        // Vector3 view = new Vector3(-1, 1, -1).normalized;
        //
        // _ball.IsBatTouch = true;
        // _ball.IsGroundBall = false;
        // _ball.IsPassing = false;
        //
        // _ball.RemovePlayer();
        //
        // float r = Random.Range(15.0f, 25.0f);
        //
        // view *= 19;
        // _ball.transform.position = Vector3.zero;
        // _ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        // _ball.GetComponent<Rigidbody>().AddForce(view, ForceMode.Impulse);
        //
        // MoveBase();
    }

    private void SwingSignalToBatter()
    {
        StartCoroutine(Swing());
    }

    IEnumerator Swing()
    {
        yield return new WaitForSeconds(1.0f);
        currentBatter.StartSwing();
    }

    #endregion


    #region ALGORITHM

    private bool ThrowToBase(int index)
    {
        if (_ball.MyDefender)
        {
            if (_ball.MyDefender == defenders[index + 1] && (0 <= index && index < 4) ) //1루수 ~ 4루수
            {
                return false;
            }
            
            _ball.MyDefender.ThrowBall(bases[index].position + new Vector3(0, 0.5f, 0));
            return true;
        }

        return false;
    }

    public int FindClosestDefenderIndex()
    {
        float min = float.MaxValue;
        int index = -1;
        for (int i = 0; i < defenders.Length; i++)
        {
            float dis = GetDistanceBetween(_ball.transform.position, defenders[i].transform.position);
            if (min > dis)
            {
                min = dis;
                index = i;
            }
        }

        if (!defenders[index].gameObject.activeSelf)
        {
            return -1;
        }

        _ball.DefenderDis = min;
        return index;
    }
    
    private bool ThrowBallAlgorithm() //SO
    {
        for (int i = runners.Length - 1; i >= 0; i--)
        {
            //has runner and run
            if (runners[i].Count > 0 && runners[i].Peek().IsMove)
            {
                if (ThrowToBase(i))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void OutBatter(int index)
    {
        //don't have runner
        if (runners[index].Count == 0)
        {
            return;
        }

        Batter batter = runners[index].Peek();
        
        //has runner and don't run
        if (!batter.IsMove)
        {
            return;
        }

        AddOut();
        
        runners[index].Dequeue();
        if (currentBatter == batter)
        {
            currentBatter = null;
        }
        Destroy(batter.gameObject);
    }

    //move one base
    void MoveOneBase()
    {
        MoveBase();

        //don't have Runner
        if (runners[0].Count == 0)
        {
            RunRunner();
        }
    }

    void MoveBase()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            //HasRunner
            if (runners[i].Count > 0)
            {
                runners[i].Peek().IsMove = true;
            }
        }
    }

    #endregion

    #region DEBUG

    void DebugBaseStatus()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            Debug.Log(i + " : " + runners[i].Count);
            if (runners[i].Count != 0)
            {
                Debug.Log("Null뜨면 애초에 Runners.push가 두번된거");
                Debug.Log(i + " : " + runners[i].Peek().name);
            }
        }
    }

    #endregion
}
