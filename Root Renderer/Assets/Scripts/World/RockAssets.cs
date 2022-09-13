using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace World
{
    [CreateAssetMenu(fileName = "RockAssets", menuName = "ScriptableObjects/RockAssets", order = 1)]

    public class RockAssets : ScriptableObject
    {
        [SerializeField] private List<GameObject> rocks = new List<GameObject>();

        // Returns a random rock asset
        public GameObject GetRandomRock()
        {
            return rocks[Random.Range(0, rocks.Count - 1)];
        }
    }
}
