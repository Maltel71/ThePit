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
    [SerializeField] private GameObject damageScreenEffect; // R�d sk�rmeffekt vid skada

    [Header("Hunger")]
    [SerializeField] private Image hungerBar;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private GameObject hungerScreenEffect; // Gul/orange sk�rmeffekt vid hunger

    [Header("D�d")]
    [SerializeField] private GameObject deathPanel;

    [Header("Effektinst�llningar")]
    [SerializeField] private float effectFadeSpeed = 2f; // Hur snabbt sk�rmeffekterna tonas ut

    // Referens till spelarens status
    private PlayerStatus playerStatus;

    // F�r att styra effekternas synlighet
    private float damageEffectAlpha = 0f;
    private float hungerEffectAlpha = 0f;

    // Effekttimer
    private float damageEffectTimer = 0f;
    private float hungerEffectTimer = 0f;

    private void Start()
    {
        // Hitta spelarens status-skript
        playerStatus = FindObjectOfType<PlayerStatus>();

        if (playerStatus != null)
        {
            // Prenumerera p� events f�r att uppdatera UI
            playerStatus.OnHealthChanged += UpdateHealthUI;
            playerStatus.OnHungerChanged += UpdateHungerUI;
            //playerStatus.OnPlayerDeath += ShowDeathScreen;
        }
        else
        {
            Debug.LogError("Kan inte hitta PlayerStatus-komponenten!");
        }

        // G�m d�dssk�rmen och sk�rmeffekter
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        // Inaktivera sk�rmeffekter fr�n b�rjan
        if (damageScreenEffect != null)
        {
            damageScreenEffect.SetActive(false);
        }

        if (hungerScreenEffect != null)
        {
            hungerScreenEffect.SetActive(false);
        }
    }

    private void Update()
    {
        // Hantera sk�rmeffekter
        HandleScreenEffects();
    }

    private void HandleScreenEffects()
    {
        // Hantera skadeeffekt
        if (damageScreenEffect != null)
        {
            if (playerStatus != null && playerStatus.IsLowHealth)
            {
                // Visa effekt n�r h�lsan �r l�g
                damageEffectTimer = 1.0f;
                damageScreenEffect.SetActive(true);
            }
            else if (damageEffectTimer > 0)
            {
                // Tona ut effekten
                damageEffectTimer -= Time.deltaTime * effectFadeSpeed;

                // Uppdatera genomskinlighet via CanvasGroup om det finns
                CanvasGroup damageGroup = damageScreenEffect.GetComponent<CanvasGroup>();
                if (damageGroup != null)
                {
                    damageGroup.alpha = damageEffectTimer;
                }

                // St�ng av effekten n�r timern n�r noll
                if (damageEffectTimer <= 0)
                {
                    damageScreenEffect.SetActive(false);
                }
            }
        }

        // Hantera hungereffekt
        if (hungerScreenEffect != null)
        {
            if (playerStatus != null && playerStatus.IsHungry)
            {
                // Visa effekt n�r spelaren �r hungrig
                hungerEffectTimer = 1.0f;
                hungerScreenEffect.SetActive(true);
            }
            else if (hungerEffectTimer > 0)
            {
                // Tona ut effekten
                hungerEffectTimer -= Time.deltaTime * effectFadeSpeed;

                // Uppdatera genomskinlighet via CanvasGroup om det finns
                CanvasGroup hungerGroup = hungerScreenEffect.GetComponent<CanvasGroup>();
                if (hungerGroup != null)
                {
                    hungerGroup.alpha = hungerEffectTimer;
                }

                // St�ng av effekten n�r timern n�r noll
                if (hungerEffectTimer <= 0)
                {
                    hungerScreenEffect.SetActive(false);
                }
            }
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
        }

        // Aktivera skadeeffekt n�r h�lsan �r l�g
        if (damageScreenEffect != null && currentHealth <= maxHealth * 0.3f)
        {
            damageScreenEffect.SetActive(true);
            damageEffectTimer = 1.0f;
        }
    }

    private void UpdateHungerUI(float currentHunger, float maxHunger)
    {
        if (hungerBar != null)
        {
            hungerBar.fillAmount = currentHunger / maxHunger;
        }

        if (hungerText != null)
        {
            hungerText.text = $"{Mathf.Ceil(currentHunger)}/{maxHunger}";
        }

        // Aktivera hungereffekt n�r hungern �r l�g
        if (hungerScreenEffect != null && currentHunger <= 10f)
        {
            hungerScreenEffect.SetActive(true);
            hungerEffectTimer = 1.0f;
        }
        else if (hungerScreenEffect != null && currentHunger > 30f)
        {
            // St�ng av hungereffekten n�r hungern �r tillr�ckligt h�g
            hungerEffectTimer = 0f;
            hungerScreenEffect.SetActive(false);
        }
    }

    // Denna metod anropas n�r spelaren d�r
    public void ShowDeathScreen()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        // Avprenumerera fr�n events f�r att undvika minnesl�ckor
        if (playerStatus != null)
        {
            playerStatus.OnHealthChanged -= UpdateHealthUI;
            playerStatus.OnHungerChanged -= UpdateHungerUI;
            //playerStatus.OnPlayerDeath -= ShowDeathScreen;
        }
    }
}