using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Chunk
{
    public Vector2Int Index;
    public BoundsInt Bounds;

    public Chunk(Vector2Int Index, float Size)
    {
        this.Index = Index;
        this.Bounds = new BoundsInt((Vector3Int)Index * (int)Size, Vector3Int.one * (int)Size);
    }
}
