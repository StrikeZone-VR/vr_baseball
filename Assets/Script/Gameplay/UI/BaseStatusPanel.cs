using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseStatusPanel : MonoBehaviour
{
    [SerializeField] private Image[] bases; //3
    [SerializeField] private Image[] baseLines; //4

    public void SetInit()
    {
        for (int i = 0; i < bases.Length; i++)
        {
            SetBase(i, false);
        }
        for (int i = 0; i < baseLines.Length; i++)
        {
            SetBaseLine(i, false);
        }
    }
    //바꿈 
    // 0 1 2, 근데 batter의 base_index는 1 2 3, 0은 아직 달리는 중
    public void SetBase(int index, bool isFull)
    {
        //입력값은 1, 2, 3
        //index 기준은 runner.
        if (index <= 0 || index > bases.Length)
        {
            return;
        }
        if (isFull)
        {
            bases[index - 1].color = Color.green;
        }
        else
        {
            bases[index - 1].color = Color.white;
        }
    }
    public void SetBaseLine(int index, bool isFull)
    {
        if (isFull)
        {
            baseLines[index].color = Color.green;
        }
        else
        {
            baseLines[index].color = Color.white;
        }
    }
}
