using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Growth
{
    [Serializable]
    public struct RootCardAsset
    {
        public StatType StatType;
        public Sprite Icon;

        public RootCardAsset(StatType StatType, Sprite Icon)
        {
            this.StatType = StatType;
            this.Icon = Icon;
        }
    }
}
