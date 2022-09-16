using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 cameraOffset;
    public bool Recapping = false;
    [SerializeField] private float recapSpeed = 8f;
    private List<Vector3> _recapLine = new List<Vector3>();
    private int _recapIndex = 0;
    private float _recapWaypointDistance = 0.01f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!Recapping)
        {
            transform.position = target.position + cameraOffset;
        }
        else
        {
            // Update the position
            transform.position = GetRecapPosition() + cameraOffset;
        }
    }
    
    // Sets up the recap timer
    public void SetRecapTimer(List<Vector3> recapLine)
    {
        // Store the line
        _recapLine = recapLine;
        _recapIndex = 0;
        
        // Set recapping to true
        Recapping = true;
    }
    
    // Returns a point on the recap line based on the recap time
    private Vector3 GetRecapPosition()
    {
        if (_recapIndex > _recapLine.Count - 1)
        {
            return Vector3.zero;
        }
        else
        {
            if (Vector3.Distance(_recapLine[_recapIndex], transform.position - cameraOffset) < _recapWaypointDistance)
            {
                _recapIndex++;
            }
            
            if (_recapIndex > _recapLine.Count - 1)
            {
                return Vector3.zero;
            }

            return Vector3.MoveTowards(transform.position - cameraOffset, _recapLine[_recapIndex], recapSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (_recapLine.Count > 0 && Recapping)
        {
            Gizmos.color = Color.cyan;
            Vector3 lastRecapPoint = _recapLine[0];
            foreach (Vector3 recapPoint in _recapLine)
            {
                Gizmos.DrawLine(lastRecapPoint, recapPoint);
                Gizmos.DrawSphere(recapPoint, 0.125f);

                lastRecapPoint = recapPoint;
            }
        }
    }
}
