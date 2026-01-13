using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattingModel : GameModel
{
    private int hit;
    private int homerun;
    private int foul;
    private int ground_ball; //땅볼

    public int Hit
    {
        get => hit;
        set => hit = value;
    }

    public int Homerun
    {
        get => homerun;
        set => homerun = value;
    }

    public int Foul
    {
        get => foul;
        set => foul = value;
    }

    public int GroundBall
    {
        get => ground_ball;
        set => ground_ball = value;
    }
}
