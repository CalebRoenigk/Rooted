using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Growth
{
    [Serializable]
    public class TreeStat
    {
        public StatType StatType;
        public float Value;
        public List<StatModifier> Modifiers = new List<StatModifier>();

        public TreeStat(float Value)
        {
            this.Value = Value;
        }
        
        // Adds a modifier to the stat
        public void AddModifier(StatModifier StatModifier)
        {
            this.Modifiers.Add(StatModifier);
        }
        
        // Returns the stat value
        public float GetValue()
        {
            float newValue = this.Value;
            
            // Sort the modifier list by operation 
            List<StatModifier> sortedModifiers = Modifiers.OrderByDescending(m => (int) (m.OperationType)).ToList();
            foreach (StatModifier statModifier in sortedModifiers)
            {
                switch (statModifier.OperationType)
                {
                    case StatOperation.Addition:
                        newValue += statModifier.Value;
                        break;
                    case StatOperation.Subtraction:
                        newValue -= statModifier.Value;
                        break;
                    case StatOperation.Multiplication:
                        newValue *= statModifier.Value;
                        break;
                    case StatOperation.Division:
                        newValue /= statModifier.Value;
                        break;
                    case StatOperation.Max:
                        newValue = Mathf.Min(newValue,statModifier.Value);
                        break;
                    default:
                        Debug.Log($"Stat Operation {statModifier.OperationType} not accounted for...");
                        break;
                }
            }
            
            return newValue;
        }
        
        // Returns a modified value based on the passed tree stat and an input value (used for things like one off score modifications
        public static float GetOneOffValue(TreeStat Stat, float Value)
        {
            float newValue = Value;
            
            // Sort the modifier list by operation 
            List<StatModifier> sortedModifiers = Stat.Modifiers.OrderByDescending(m => (int) (m.OperationType)).ToList();
            foreach (StatModifier statModifier in sortedModifiers)
            {
                switch (statModifier.OperationType)
                {
                    case StatOperation.Addition:
                        newValue += statModifier.Value;
                        break;
                    case StatOperation.Subtraction:
                        newValue -= statModifier.Value;
                        break;
                    case StatOperation.Multiplication:
                        newValue *= statModifier.Value;
                        break;
                    case StatOperation.Division:
                        newValue /= statModifier.Value;
                        break;
                    case StatOperation.Max:
                        newValue = Mathf.Min(newValue,statModifier.Value);
                        break;
                    default:
                        Debug.Log($"Stat Operation {statModifier.OperationType} not accounted for...");
                        break;
                }
            }

            return newValue;
        }
    }
}
