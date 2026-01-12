using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseballModel : GameModel
{
    private int ball_count = 0;
    private int strike_count = 0;
    private int out_count = 0; //흐음.
    
    //define
    private const int MAX_BALL_COUNT = 4;
    private const int MAX_STRIKE_COUNT = 3;
    private const int MAX_OUT_COUNT = 3;
    
    //대충 Controller에 있는 옵서버 함수
    //SO 함수는 생성시점이 불안함
    
    //property
    public int OutCount
    {
        get { return out_count; }
        set
        {
            out_count = value;

            BallCount = 0;
            Strike = 0;

            Debug.Log("아웃 : " + out_count);
            if (out_count >= MAX_OUT_COUNT)
            {
                out_count = 0;
                Inning++;
            }

            _UIGameStatusElements[2].SetIndex(out_count);
        }
    }
    
    private void AddOut()
    {
        OutCount++;
    }

}
