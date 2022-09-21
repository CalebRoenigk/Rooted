using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Growth
{
    public class TreeCard : MonoBehaviour
    {
        [Header("Runtime")]
        public StatType StatType;
        public StatModifier Modifier;

        [SerializeField] private TextMeshProUGUI cardText;
        [SerializeField] private Image cardImage;
        [SerializeField] private RootCardAssets cardAssets;
        
        
        // Set the card settings
        public void SetCard(StatModifier statModifier, StatType statType)
        {
            this.StatType = statType;
            this.Modifier = statModifier;
            
            // Set the text
            cardText.text = statModifier.ToString(statType);
            
            // Set the icon
            cardImage.sprite = cardAssets.GetStatSprite(statType);
        }

        private void Start()
        {
            SetCard(this.Modifier, this.StatType);
        }
    }
}
