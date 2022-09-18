using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Growth
{
    public class TreeCard : MonoBehaviour
    {
        [Header("Runtime")]
        public StatType StatType;
        public StatModifier Modifier;

        [SerializeField] private TextMeshProUGUI cardText;
        
        
        // Set the card settings
        public void SetCard(StatModifier statModifier, StatType statType)
        {
            this.StatType = statType;
            this.Modifier = statModifier;
            
            // Set the text
            cardText.text = statModifier.ToString(statType);
        }

        private void Start()
        {
            SetCard(this.Modifier, this.StatType);
        }
    }
}
