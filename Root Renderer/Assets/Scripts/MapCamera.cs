using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    private Vector3 _offset = new Vector3(0f, 0f, -10f);

    private void FixedUpdate()
    {
        transform.position = target.position + _offset;
    }
}
