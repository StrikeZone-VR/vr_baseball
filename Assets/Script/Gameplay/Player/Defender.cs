using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//수비수
public class Defender : Player
{


    //position => direction
    public void ThrowBall(Vector3 position)
    {
        LookAtPlayer(position);
        
        float x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        float z = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        //cal force
    }
}
