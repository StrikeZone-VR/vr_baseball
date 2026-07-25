using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerComponent : MonoBehaviour
{
    [SerializeField] protected Player player;

    private void Start()
    {
        player = GetComponent<Player>();
    }
}