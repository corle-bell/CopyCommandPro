using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SerializableTest
{
    public int IntValue;
    public float floatValue;
    public string stringValue;
    public Vector3 vecValue;
}

public class ClassBasic : MonoBehaviour
{
    public int IntValue;
    public float floatValue;
    public string stringValue;
    public Vector3 vecValue;
    
    public int[] IntArray;

    public SerializableTest classTest;

    [SerializeField]
    private float SerializeFieldFloat;
}