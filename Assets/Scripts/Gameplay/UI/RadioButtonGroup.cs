using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadioButtonGroup : MonoBehaviour
{
    private List<Toggle> buttons;
    private int selectedIndex = 0;
    void Start()
    {
        buttons = new List<Toggle>();
        for (int i = 0; i < transform.childCount; i++)
        {
            buttons.Add(transform.GetChild(i).GetComponent<Toggle>());
        }
        buttons[selectedIndex].Select();
    }

}
