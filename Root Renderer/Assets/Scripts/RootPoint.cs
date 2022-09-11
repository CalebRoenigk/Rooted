using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RootPoint
{
    public Vector3 Position;
    public float Width;

    public RootPoint(Vector3 Position, float Width)
    {
        this.Position = Position;
        this.Width = Width;
    }
}
