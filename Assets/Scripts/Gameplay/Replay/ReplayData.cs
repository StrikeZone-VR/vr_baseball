using System.Collections.Generic;
using UnityEngine;

//한 번의 위치/회전 키. 연속 데이터(transform)는 이걸로 프레임마다 저장한다.
[System.Serializable]
public struct PoseKey
{
    public Vector3 pos;
    public Quaternion rot;

    public PoseKey(Transform t)
    {
        pos = t.position;
        rot = t.rotation;
    }
}

//매 프레임 저장하는 조밀(dense) 트랙. ball/bat/양손/주자의 transform.
[System.Serializable]
public struct TransformFrame
{
    public float time;
    public PoseKey ball;
    public PoseKey bat;
    public PoseKey leftHand;
    public PoseKey rightHand;
    public PoseKey head;        //HMD 카메라(플레이어 머리). 아바타/플레이어 시점 재생용
    public PoseKey[] runners;   //gamePlayModel.GetRunners() 순서와 동일
    public PoseKey[] defenders; //수비수(투수=0 ... 포수=4). GetDefenderTransforms() 순서와 동일
}

//상태가 "변할 때만" 저장하는 희소(sparse) 키프레임. 깃 커밋처럼 변경분만 쌓인다.
//재생 때는 현재 시간 이하의 마지막 키프레임을 읽으면 그 시점 상태가 된다.
[System.Serializable]
public struct StatusKeyframe
{
    public float time;
    public GamePlayManager.StatusSnapshot status;
}

//strike/foul/homerun 같은 순간 이벤트 마커(자막·타임라인 점프용). 희소.
[System.Serializable]
public struct EventMarker
{
    public float time;
    public string type;
}

//GameLog가 콘솔에 찍는 한 줄을 시간과 함께 저장한 것. 재생 씬 상태 패널에 로그로 흘려보낸다. 희소.
//msg에는 richText <color> 태그가 그대로 들어있어(예: "<color=cyan>[Ball]</color> ...") 패널에서 색이 유지된다.
[System.Serializable]
public struct LogEntry
{
    public float time;
    public string msg;
}

//한 게임 세션의 리플레이 전체. ScriptableObject라 .asset으로 저장해 다른 씬에서 불러올 수 있다.
//Project 창 우클릭 → Create > Model > Replay Data 로 .asset 생성.
[CreateAssetMenu(fileName = "NewReplay", menuName = "Model/Replay Data")]
public class ReplayData : ScriptableObject
{
    [HideInInspector] public List<TransformFrame> frames = new List<TransformFrame>();   //조밀 — 매 프레임(수천 개 → 인스펙터 숨김)
    public List<StatusKeyframe> statusTrack = new List<StatusKeyframe>(); //희소 — 변할 때만
    public List<EventMarker> events = new List<EventMarker>();         //희소 — 순간 이벤트
    public List<LogEntry> logs = new List<LogEntry>();                 //희소 — GameLog 콘솔 로그
}
