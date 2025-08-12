using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGameStatus : MonoBehaviour
{
    [SerializeField] private Image [] elements;
    [SerializeField] private Color _color;

    public void SetIndex(int index)
    {
        int i;
        for (i = 0; i < index; i++)
        {
            Debug.Log("엄준식");
            elements[i].color = _color;
        }

        //black
        for (; i < elements.Length; i++)
        {
            elements[i].color = Color.black;
        }
    }

}
