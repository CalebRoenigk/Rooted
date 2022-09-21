using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Growth
{
    [CreateAssetMenu(fileName = "RootCardAssets", menuName = "ScriptableObjects/RootCardAssets", order = 2)]
    public class RootCardAssets : ScriptableObject
    {
        public List<RootCardAsset> RootCardResources = new List<RootCardAsset>();
        
        // Returns the sprite for the stat modifier based on the passed type
        public Sprite GetStatSprite(StatType type)
        {
            return RootCardResources.Find(a => a.StatType == type).Icon;
        }
    }
}
