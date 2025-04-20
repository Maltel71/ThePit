using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatus : MonoBehaviour
{
    [Header("H�lsa")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private float fallDamageThreshold = 10f; // Hastighet som kr�vs f�r att ta fallskada
    [SerializeField] private float fallDamageMultiplier = 5f; // Hur mycket fallskada som tas per hastighets�kning

    [Header("Hunger")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger;
    [SerializeField] private Image hungerBar;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private float hungerDecreaseRate = 0.5f; // Hur mycket hunger minskar per sekund
    [SerializeField] private float hungerDamageThreshold = 0f; // N�r spelaren b�rjar ta skada av hunger
    [SerializeField] private float hungerDamageRate = 2f; // Skada per sekund vid hunger

    [Header("Ljud")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip eatSound;
    [SerializeField] private AudioClip deathSound;

    private CharacterController controller;
    private AudioSource audioSource;
    private bool isDead = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        // L�gg till AudioSource om det saknas
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // Initiera h�lsa och hunger till max
        currentHealth = maxHealth;
        currentHunger = maxHunger;

        // Uppdatera UI
        UpdateHealthUI();
        UpdateHungerUI();
    }

    private void Update()
    {
        if (isDead)
            return;

        // Hantera hunger �ver tid
        HandleHunger();
    }

    private void HandleHunger()
    {
        // Minska hunger �ver tid
        currentHunger -= hungerDecreaseRate * Time.deltaTime;
        currentHunger = Mathf.Max(0, currentHunger);

        // Ta skada om hunger �r under tr�skelv�rdet
        if (currentHunger <= hungerDamageThreshold)
        {
            TakeDamage(hungerDamageRate * Time.deltaTime);
        }

        // Uppdatera UI
        UpdateHungerUI();
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        // Spela ljud
        if (damageSound != null && damage > 1f)
        {
            audioSource.PlayOneShot(damageSound);
        }

        // Uppdatera UI
        UpdateHealthUI();

        // Kontrollera om spelaren har d�tt
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Eat(float foodValue)
    {
        currentHunger += foodValue;
        currentHunger = Mathf.Min(currentHunger, maxHunger);

        // Spela ljud
        if (eatSound != null)
        {
            audioSource.PlayOneShot(eatSound);
        }

        // Uppdatera UI
        UpdateHungerUI();
    }

    public void CheckFallDamage(float impactVelocity)
    {
        // Om hastigheten �r negativ (faller) och �ver tr�skelv�rdet
        if (impactVelocity < -fallDamageThreshold)
        {
            float damage = Mathf.Abs(impactVelocity + fallDamageThreshold) * fallDamageMultiplier;
            TakeDamage(damage);
            Debug.Log($"Fallskada: {damage} fr�n hastighet: {impactVelocity}");
        }
    }

    private void Die()
    {
        isDead = true;

        // Spela ljud
        if (deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        Debug.Log("Spelaren har d�tt!");

        // H�r kan du l�gga till mer d�dlogik, t.ex. spela en animation, visa game over, etc.
        // F�r enkelhets skull, laddar vi bara om scenen efter en liten f�rdr�jning
        StartCoroutine(ReloadSceneAfterDelay(3f));
    }

    private IEnumerator ReloadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
        }
    }

    private void UpdateHungerUI()
    {
        if (hungerBar != null)
        {
            hungerBar.fillAmount = currentHunger / maxHunger;
        }

        if (hungerText != null)
        {
            hungerText.text = $"{Mathf.Ceil(currentHunger)}/{maxHunger}";
        }
    }

    public void RestoreHealth(float amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        // Uppdatera UI
        UpdateHealthUI();
    }

    public void SetUIReferences(Image healthBarRef, TextMeshProUGUI healthTextRef,
                             Image hungerBarRef, TextMeshProUGUI hungerTextRef)
    {
        healthBar = healthBarRef;
        healthText = healthTextRef;
        hungerBar = hungerBarRef;
        hungerText = hungerTextRef;

        // Uppdatera UI med aktuella v�rden
        UpdateHealthUI();
        UpdateHungerUI();
    }
}