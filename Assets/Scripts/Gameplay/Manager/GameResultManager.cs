using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResultManager : MonoBehaviour
{
    [SerializeField] private ResultPanelController resultPanel;

    [SerializeField] private Vector3 initPosition;
    [SerializeField] private Vector3 initRotation;

    [Header("Listening to EventChannels")] 
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;
    void Start()
    {
        resultPanel.UpdateResultUI();

        moveOriginEvent.RaiseEvent(initPosition);
        rotateOriginEvent.RaiseEvent(initRotation);
    }
}
