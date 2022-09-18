using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Growth
{
    [Serializable]
    public class TreeStats
    {
        public TreeStat Energy;
        public TreeStat GrowthSpeed;
        public TreeStat GrowthManuverability;
        public TreeStat ExtraRoot;
        public TreeStat Scoring;
        
        // Adds a stat to the tree
        public void AddModifer(StatType statType, StatModifier statModifier)
        {
            switch (statType)
            {
                case StatType.Energy:
                    Energy.AddModifier(statModifier);
                    break;
                case StatType.GrowthSpeed:
                    GrowthSpeed.AddModifier(statModifier);
                    break;
                case StatType.GrowthManuverability:
                    GrowthManuverability.AddModifier(statModifier);
                    break;
                case StatType.ExtraRoot:
                    ExtraRoot.AddModifier(statModifier);
                    break;
                case StatType.Scoring:
                    Scoring.AddModifier(statModifier);
                    break;
                case StatType.None:
                default:
                    break;
            }
        }
    }
}
