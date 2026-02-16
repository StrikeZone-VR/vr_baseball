using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseStatusPanel : MonoBehaviour
{
    [SerializeField] private Image[] bases; //3
    [SerializeField] private Image[] baseLines; //4


    public void SetBase(int index, bool isFull)
    {
        if (index < 0 || index >= bases.Length)
        {
            return;
        }
        if (isFull)
        {
            bases[index].color = Color.green;
        }
        else
        {
            bases[index].color = Color.white;
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
