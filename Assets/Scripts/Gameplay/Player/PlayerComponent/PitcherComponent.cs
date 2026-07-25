using System.Collections;
using UnityEngine;

public class PitcherComponent : DefenderComponent
{
    private const float ADDFORCE = 20.0f;
    private Coroutine coroutine;
    [SerializeField] private StrikeZone strikeZone;

    [SerializeField] private VoidEventSO swingEvent; //from GameManager
    [SerializeField] private IntEventSO waitPitcherEvent; //from BattingSystem

    [SerializeField] private float velocityXZ = 40;
    //_myBall

    //이 투수가 어떤 구종을 어디로 얼마나 빠르게 던지는지에 대한 데이터.
    //인스펙터로 박지 않고 BattingController가 넣어준다 (선수 교체를 씬 수정 없이 하려고).
    //비어 있으면 예전처럼 정 가운데(zone 4)로 velocityXZ 속력으로만 던진다.
    private PitcherSO pitcherSO;

    public PitcherSO PitcherData
    {
        get => pitcherSO;
        set => pitcherSO = value;
    }

    const int WAIT_TIME = 5; //5.0f
    protected bool isThrowBallStop = false; //debug
    
    const float ARRIVE_DISTANCE = 0.2f;
        
    void Awake()
    {
        //투수는 LookAtPlayer로만 회전 제어. nav가 velocity 잔류로 덮어쓰는 거 차단.
        //if (player == null) player = GetComponent<Player>();
        //player.SetNavUpdateRotation(false);
    }

    protected override void Update()
    {
        float dis = Vector3.Distance(defenderTransform.position, transform.position);

        if (dis <= ARRIVE_DISTANCE)
        {
            IsInPosition = true;
        }
        else
        {
            IsInPosition = false;
        }
        
        base.Update();
    }
    


    //protected override void Update()
    //{
    //    base.Update();
    //    if (Input.GetKeyDown(KeyCode.Space))
    //        PitchBall();
    //}


    //이걸로 공 설정해라
    public override void SetMyBall(Baseball myBall)
    {
        base.SetMyBall(myBall);
        
        //만약 배트가 터치됐다면 => 경기중
        if (myBall.IsInGamePlay)
        {
            return;
        }
        StopPitching();
        coroutine = StartCoroutine(WaitPitching());
        
        //Debug.Log("음? : "+_ball.MyDefender.name);
        //transform.LookAt(_ball.transform, Vector3.up);
    }

    IEnumerator WaitPitching()
    {
        //멈춰야 하거나 내 공이 없다면
        if (IsThrowBallStop || !_myBall)
        {
            yield break;
        }

        //원래 isPosition으로 하려고 했지만 
        //if (Vector3.Distance(transform.position, defenderTransform.position) > ARRIVE_DISTANCE)
        if(!IsInPosition)
        {
            player.MovePlayer(defenderTransform.position);
            while (!IsInPosition)
                yield return null;
        }
            
        //nav가 회전 덮어쓰지 않도록 정지
        player.StopMove();
        LookAtPlayer(strikeZone.transform.position);

        //5임
        for (int i = WAIT_TIME; i > 0; i--)
        {
            waitPitcherEvent.RaiseEvent(i);
            yield return new WaitForSeconds(1.0f);
        }

        coroutine = null;
        ThrowByPitcherData();
    }

    /// <summary>
    /// PitcherSO를 기준으로 구종 / 코스 / 구속을 뽑아서 던진다.
    /// SO가 안 꽂혀 있거나 등록된 구종이 없으면 예전 동작(정 가운데 + velocityXZ)으로 폴백한다.
    /// ㄴ 씬 배선을 안 건드려도 기존과 똑같이 굴러가게 하려는 것
    /// </summary>
    private void ThrowByPitcherData()
    {
        //기본값 = 정 가운데 (zones[12] = MiddleCenter)
        Vector3 targetPosition = strikeZone.ZoneCount > StrikeZone.CENTER_ZONE_INDEX
            ? strikeZone.GetZone(StrikeZone.CENTER_ZONE_INDEX).position //랜덤넣자
            : strikeZone.transform.position;

        float speed = velocityXZ;

        PitchingPlayerData data = pitcherSO != null ? pitcherSO.PickPitchingData() : null;

        if (data != null)
        {
            //구종을 "던지기 전에" 정해야 한다.
            //ㄴ BaseballPhysics.ThrowBall이 던지는 순간 Baseball.GetSelectedPitchTypeSO()의
            //   ForceWeight를 읽어 궤적을 계산하므로, 순서가 바뀌면 직전 구종으로 날아간다.
            _myBall.SetPitchType(data.Type);

            int zoneIndex = data.PickZoneIndex();
            Transform zone = (zoneIndex >= 0 && zoneIndex < strikeZone.ZoneCount)
                ? strikeZone.GetZone(zoneIndex)
                : null;

            if (zone != null)
            {
                targetPosition = zone.position;
            }

            //구속을 입력 안 한 구종은 0을 돌려주므로 그때는 velocityXZ를 그대로 쓴다
            float picked = data.PickVelocity();
            if (picked > 0f)
            {
                speed = picked;
            }

            //존 이름과 스트라이크 여부는 씬 오브젝트에서 직접 읽는다.
            //ㄴ 실제 판정도 BaseballPhysics가 같은 "StrikeZone" 태그를 보므로,
            //   상수를 따로 두는 것보다 로그와 실제 판정이 어긋날 일이 없다.
            string zoneName = zone != null ? zone.name : "없음";
            string judge = (zone != null && zone.CompareTag("StrikeZone")) ? "스트라이크존" : "볼존";

            GameLog.Pitch(
                $"[Pitcher] 구종 {data.Type} / 구속 {speed:F1}km/h / 코스 [{zoneIndex}] {zoneName} ({judge})");
        }

        _myBall.ThrowBall(
            _myBall.transform.position,
            targetPosition,
            speed,
            true
        );
    }



    public float VelocityXZ
    {
        set
        {
            if (value <= 0 || 200 <= value)
            {
                return;
            }
            velocityXZ = value;
            Debug.Log("속력 설정 : " + velocityXZ);
        }
        get { return velocityXZ; }
    }

    public bool IsThrowBallStop
    {
        get => isThrowBallStop;
        set
        {
            isThrowBallStop = value;
            
            StopPitching();
            //어차피 WaitPitching에서 판별해준다. 
            coroutine = StartCoroutine(WaitPitching());
        }
    }
    public void StopPitching()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine); 
        }
    }
}
