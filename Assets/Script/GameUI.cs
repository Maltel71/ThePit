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
    [SerializeField] private GameObject damageScreenEffect; // Röd skärmeffekt vid skada

    [Header("Hunger")]
    [SerializeField] private Image hungerBar;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private GameObject hungerScreenEffect; // Gul/orange skärmeffekt vid hunger

    [Header("Död")]
    [SerializeField] private GameObject deathPanel;

    [Header("Effektinställningar")]
    [SerializeField] private float effectFadeSpeed = 2f; // Hur snabbt skärmeffekterna tonas ut

    // Referens till spelarens status
    private PlayerStatus playerStatus;

    // För att styra effekternas synlighet
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
            // Prenumerera på events för att uppdatera UI
            playerStatus.OnHealthChanged += UpdateHealthUI;
            playerStatus.OnHungerChanged += UpdateHungerUI;
            //playerStatus.OnPlayerDeath += ShowDeathScreen;
        }
        else
        {
            Debug.LogError("Kan inte hitta PlayerStatus-komponenten!");
        }

        // Göm dödsskärmen och skärmeffekter
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        // Inaktivera skärmeffekter från början
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
        // Hantera skärmeffekter
        HandleScreenEffects();
    }

    private void HandleScreenEffects()
    {
        // Hantera skadeeffekt
        if (damageScreenEffect != null)
        {
            if (playerStatus != null && playerStatus.IsLowHealth)
            {
                // Visa effekt när hälsan är låg
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

                // Stäng av effekten när timern når noll
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
                // Visa effekt när spelaren är hungrig
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

                // Stäng av effekten när timern når noll
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

        // Aktivera skadeeffekt när hälsan är låg
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

        // Aktivera hungereffekt när hungern är låg
        if (hungerScreenEffect != null && currentHunger <= 10f)
        {
            hungerScreenEffect.SetActive(true);
            hungerEffectTimer = 1.0f;
        }
        else if (hungerScreenEffect != null && currentHunger > 30f)
        {
            // Stäng av hungereffekten när hungern är tillräckligt hög
            hungerEffectTimer = 0f;
            hungerScreenEffect.SetActive(false);
        }
    }

    // Denna metod anropas när spelaren dör
    public void ShowDeathScreen()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        // Avprenumerera från events för att undvika minnesläckor
        if (playerStatus != null)
        {
            playerStatus.OnHealthChanged -= UpdateHealthUI;
            playerStatus.OnHungerChanged -= UpdateHungerUI;
            //playerStatus.OnPlayerDeath -= ShowDeathScreen;
        }
    }
}