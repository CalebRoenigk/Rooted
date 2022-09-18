using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Growth
{
    [Serializable]
    public class StatModifier
    {
        public StatOperation OperationType;
        public float Value;
        
        // Returns the modifier as a string
        public string ToString(StatType statType)
        {
            string operation = "";
            switch (OperationType)
            {
                case StatOperation.Addition:
                    operation = "+";
                    break;
                case StatOperation.Subtraction:
                    operation = "-";
                    break;
                case StatOperation.Multiplication:
                    operation = "x";
                    break;
                case StatOperation.Division:
                    operation = "÷";
                    break;
                case StatOperation.Max:
                    operation = "Max of ";
                    break;
            }

            return $"{statType.ToString()} {operation}{Value}";
        }
    }
}
