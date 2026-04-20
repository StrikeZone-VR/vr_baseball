using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResultManager : MonoBehaviour
{
    [SerializeField] private ResultPanelController resultPanel;
    void Start()
    {
        resultPanel.UpdateResultUI();
    }
}
