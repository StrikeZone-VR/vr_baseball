using UnityEngine;


[CreateAssetMenu(fileName = "NewBattingData", menuName = "Model/Batting Data")]
public class BattingModel : GameModel
{
    [SerializeField] private int hit;
    [SerializeField] private int homerun;
    [SerializeField] private int foul;
    [SerializeField] private int ground_ball; //땅볼

    //이 값들을 전달해야함
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

    //
    public void Init()
    {
        hit = 0;
        homerun = 0;
        foul = 0;
        ground_ball = 0;
    }
    
}
