using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Growth
{
    public class Tree : MonoBehaviour
    {
        [Header("Level")]
        public float LevelScalar = 0.04f;
        public float LevelVerticalOffset = 1.7f;
        public float LevelBase = 200f;
        public float LevelHorizontalOffset = 2350f;
        
        [Header("Score")]
        [SerializeField] private Slider mainScore;
        [SerializeField] private Slider rootScore;
        [SerializeField] private TextMeshProUGUI levelCounter;

        [Header("Stats")]
        [SerializeField] public TreeStats TreeStats;
        
        public int Score
        {
            get { return _score; }
            set { _score = value; UpdateScore(); }
        }
        public int RootScore
        {
            get { return _rootScore; }
            set { _rootScore = value; UpdateScore(); }
        }

        private int _currentLevel = -1;
        private int _score;
        private int _rootScore;
        private int _levelScore;
        private int _evolveScore;
        
        public int RootsRemaining
        {
            get { return _rootsRemaining; }
            set { _rootsRemaining = value;}
        }

        private int _rootsRemaining;

        private void Start()
        {
            EvolveTree();
            RootsRemaining = 4;
        }

        private void Update()
        {
            if (_score >= _evolveScore)
            {
                EvolveTree();
            }
        }

        // Returns the experience needed for an input level
        public int GetExperienceForLevel(int level)
        {
            return Mathf.RoundToInt(Mathf.Pow(LevelBase, (LevelScalar * level) + LevelVerticalOffset) + LevelHorizontalOffset);
        }
        
        // Evolves the tree
        private void EvolveTree()
        {
            _currentLevel++;
            _evolveScore = GetExperienceForLevel(_currentLevel + 1);
            _levelScore = GetExperienceForLevel(_currentLevel);
            
            // Update the level UI
            levelCounter.text = $"Level: {_currentLevel}";
        }
        
        // Updates the score
        private void UpdateScore()
        {
            // Determine the progression thru the current range
            float progression = RemapFloat(_score, _levelScore, _evolveScore, 0f, 1f);
            
            // Update the main score slider
            mainScore.value = progression;
            
            // Determine the progression thru the current range as an additional root score
            float rootProgression = RemapFloat(_score + _rootScore, _levelScore, _evolveScore, 0f, 1f);
            
            // Update the main score slider
            rootScore.value = rootProgression;
            
            // Updating score
            Debug.Log($"Updating Score: {Score} + {RootScore}");
        }
        
        // Remaps a float
        public float RemapFloat(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
        
        // Store the root score to the main score
        public void StoreRoot()
        {
            Score += RootScore;
            RootScore = 0;
        }
        
        // TODO: Method to add new roots to the root count of the tree when it evolves
        
        // TODO: Stat System
        // Card/stat system
        // Types of improvements
        // Stat cards: Only positive stat increases
        // Growth cards: Cards that give stat increases but at the cost of another stat

        // Stats: Growth Total Energy, Growth Speed, Extra Root, flat bonus points, point scaling, better maneuverability
        // Growth Cards: More total energy but slower, Faster but less roots, get faster and have more energy but root total is capped to a value, You can split once per root run but your energy total is halved, speed is lowered but your points are worth more
    }
}
