using UnityEngine;

public partial class GamePlayManager
{
    //리플레이 레코더가 매 프레임 주자들의 transform을 가져갈 수 있게 노출한다.
    //주자는 동적 리스트라 인스펙터로 연결할 수 없어서 매니저가 넘겨준다.
    //반환 순서는 CaptureStatus()의 runners와 동일하다(둘 다 gamePlayModel.GetRunners()).
    public Transform[] GetRunnerTransforms()
    {
        var runners = gamePlayModel.GetRunners();
        Transform[] result = new Transform[runners.Count];
        for (int i = 0; i < runners.Count; i++)
            result[i] = runners[i].transform;
        return result;
    }
}
