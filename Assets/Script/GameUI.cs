using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Hälsa")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Hunger")]
    [SerializeField] private Image hungerBar;
    [SerializeField] private TextMeshProUGUI hungerText;

    [Header("Död")]
    [SerializeField] private GameObject deathPanel;

    private void Start()
    {
        // Hitta spelarens status-skript
        PlayerStatus playerStatus = FindObjectOfType<PlayerStatus>();

        if (playerStatus != null)
        {
            playerStatus.SetUIReferences(healthBar, healthText, hungerBar, hungerText);
        }
        else
        {
            Debug.LogError("Kan inte hitta PlayerStatus-komponenten!");
        }

        // Göm dödsskärmen om den finns
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    // Denna metod kan anropas från PlayerStatus när spelaren dör
    public void ShowDeathScreen()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
    }
}