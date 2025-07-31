using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Batter : Player
{
    //[SerializeField] private Baseball _ball;

    public void DebugHitting()
    {
        _ball.RemovePlayer();

        _myBall.transform.position = transform.position + new Vector3(0.0f, 0.5f, 0.0f);
    }

}
