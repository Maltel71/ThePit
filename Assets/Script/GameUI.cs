using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("H�lsa")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Hunger")]
    [SerializeField] private Image hungerBar;
    [SerializeField] private TextMeshProUGUI hungerText;

    [Header("D�d")]
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

        // G�m d�dssk�rmen om den finns
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    // Denna metod kan anropas fr�n PlayerStatus n�r spelaren d�r
    public void ShowDeathScreen()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
    }
}