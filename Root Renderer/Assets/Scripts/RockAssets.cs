using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RockAssets", menuName = "ScriptableObjects/RockAssets", order = 1)]
public class RockAssets : ScriptableObject
{
    [SerializeField] private List<GameObject> rocks = new List<GameObject>();
    
    // Returns a random rock asset
    public GameObject GetRandomRock()
    {
        return rocks[UnityEngine.Random.Range(0, rocks.Count - 1)];
    }
}
