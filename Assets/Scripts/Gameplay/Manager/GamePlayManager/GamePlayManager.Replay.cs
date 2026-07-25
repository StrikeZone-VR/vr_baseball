using UnityEngine;

public partial class GamePlayManager
{
    //리플레이 레코더/플레이어가 인스펙터 연결 없이 런타임에 공/배트를 가져갈 수 있게 노출한다.
    //공(_ball)은 부모 GameManager의 protected 필드, 배트(_bat)는 이 클래스의 필드.
    //override인 이유: 레코더가 GameManager 기준으로 녹화하도록 바뀌어서,
    //단독 씬(Batting/Pitching)은 GameManager의 기본 구현을 쓰고 여기서만 진짜 값을 준다.
    public override Baseball Ball => _ball;
    public override Bat Bat => _bat;

    //리플레이 레코더가 매 프레임 주자들의 transform을 가져갈 수 있게 노출한다.
    //주자는 동적 리스트라 인스펙터로 연결할 수 없어서 매니저가 넘겨준다.
    //반환 순서는 CaptureStatus()의 runners와 동일하다(둘 다 gamePlayModel.GetRunners()).
    public override Transform[] GetRunnerTransforms()
    {
        var runners = gamePlayModel.GetRunners();
        Transform[] result = new Transform[runners.Count];
        for (int i = 0; i < runners.Count; i++)
            result[i] = runners[i].transform;
        return result;
    }

    //리플레이 레코더가 매 프레임 수비수들의 transform을 가져갈 수 있게 노출한다.
    //defenders는 고정 배열이라 순서가 항상 같다(0=투수 ... 4=포수). 투수도 여기에 포함된다.
    public override Transform[] GetDefenderTransforms()
    {
        Transform[] result = new Transform[defenders.Length];
        for (int i = 0; i < defenders.Length; i++)
            result[i] = defenders[i] != null ? defenders[i].transform : null;
        return result;
    }
}
