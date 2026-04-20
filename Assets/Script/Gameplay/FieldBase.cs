using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldBase : MonoBehaviour
{
    [SerializeField] private int base_index;
    public int BaseIndex
    {
        get { return base_index; }
    }
}
