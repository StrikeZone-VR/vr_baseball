using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseballModel : GameModel
{
    private int ball_count = 0;
    private int strike_count = 0;
    
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

}
