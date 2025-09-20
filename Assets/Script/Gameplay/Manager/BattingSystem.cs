using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattingSystem : MonoBehaviour
{
    [SerializeField] private Baseball _ball;
    [SerializeField] private Pitcher pitcher;

    private void Start()
    {
        pitcher.SetMyBall(_ball);
        StartCoroutine(WaitPitching());
    }

    IEnumerator WaitPitching()
    {
        yield return new WaitForSeconds(5f);
        pitcher.SetMyBall(_ball);
        StartCoroutine(WaitPitching());
    }
    //all base ball script
    //paul to gamemanager
    //
}
