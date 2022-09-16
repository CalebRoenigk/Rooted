using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform baseNode;

    private void FixedUpdate()
    {
        // First get the offset from the target to origin
        Vector3 targetOffset = -target.position;
        
        // Move the base node
        baseNode.transform.position = targetOffset;
    }
}
