using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Growth;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject recap;
    [SerializeField] private TextMeshProUGUI lengthStat;
    [SerializeField] private TextMeshProUGUI rootStat;
    [SerializeField] private PlayerController playerController;

    // Displays the recap
    public void DisplayRecap(PlayerController.DayStats dayStats)
    {
        recap.SetActive(true);
        lengthStat.text = (Mathf.Round(dayStats.DistanceGrown * 10f) / 10f).ToString() + "ft";
        rootStat.text = dayStats.RootsMade.ToString();
    }
    
    // Starts the next day
    public void StartNextDay()
    {
        recap.SetActive(false);
        playerController.StartDay();
    }
}
