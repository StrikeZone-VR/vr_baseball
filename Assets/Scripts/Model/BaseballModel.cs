using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBaseballData", menuName = "Model/Baseball Data")]
public class BaseballModel : GameModel
{
    //근데 생각해보니까 ball_count는 정보를 전달 안해도 되지않을까
    
    [SerializeField] private int ball_count = 0;
    [SerializeField] private int strike_count = 0;
    
    //define
    public const int MAX_BALL_COUNT = 4;
    public const int MAX_STRIKE_COUNT = 3;
    
    //대충 Controller에 있는 옵서버 함수
    //SO 함수는 생성시점이 불안함
    
    public int Strike
    {
        get { return strike_count; }
        set
        {
            //상태저장
            strike_count = value;
        }
    }

    public int BallCount
    {
        get { return ball_count; }
        set
        {
            ball_count = value;
        }
    }

    public override void Init()
    {
        ball_count = 0;
        strike_count = 0;
    }

}
