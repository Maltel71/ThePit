using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    // Event för UI-uppdateringar
    public delegate void HealthChangedDelegate(float currentHealth, float maxHealth);
    public event HealthChangedDelegate OnHealthChanged;

    public delegate void HungerChangedDelegate(float currentHunger, float maxHunger);
    public event HungerChangedDelegate OnHungerChanged;

    public delegate void PlayerDeathDelegate(DeathReason reason);
    public event PlayerDeathDelegate OnPlayerDeath;

    [Header("Hälsa")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Fallskada")]
    [SerializeField] private bool enableFallDamage = true;
    [SerializeField] private float fallDamageThreshold = 10f; // Hastighet som krävs för att ta fallskada
    [SerializeField] private float fallDamageMultiplier = 5f; // Hur mycket fallskada som tas per hastighetsökning
    [SerializeField] private float minFallDamage = 5f; // Minsta möjliga fallskada
    [SerializeField] private float maxFallDamage = 50f; // Högsta möjliga fallskada

    [Header("Stridsskada")]
    [SerializeField] private float combatDamageMultiplier = 1.0f; // Multiplikator för stridsskada (för svårighetsgrad)
    [SerializeField] private float damageImmunityTime = 0.5f; // Tid efter att ha tagit skada då spelaren är immun (för att undvika flera skador i följd)
    private float lastDamageTime; // Senaste tidpunkt då spelaren tog skada

    [Header("Hunger")]
    [SerializeField] private bool enableHungerSystem = true;
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger;
    [SerializeField] private float hungerDecreaseRate = 0.5f; // Hur mycket hunger minskar per sekund
    [SerializeField] private float hungerDecreaseMultiplier = 1.5f; // Hunger minskar snabbare vid springing
    [SerializeField] private float hungerDamageRate = 2f; // Skada per sekund vid hunger

    [Header("Ljud")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip hungerSound; // Magsurr vid hunger
    [SerializeField] private AudioClip eatSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip fallDamageSound;

    // Dödsorsaker
    public enum DeathReason
    {
        Health, // Död av skada
        Hunger, // Död av svält
        Fall    // Död av fallskada
    }

    // Publika properties för UI-information
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentHunger => currentHunger;
    public float MaxHunger => maxHunger;
    public bool IsDead => isDead;
    public bool IsHungry => currentHunger <= 10f;
    public bool IsLowHealth => currentHealth <= maxHealth * 0.3f;

    private CharacterController controller;
    private AudioSource audioSource;
    private bool isDead = false;
    private float hungerSoundTimer = 0f; // Timer för att inte spela hungersljud för ofta

    // Referens till ThirdPersonController för att påverka rörelsehastighet baserat på hunger
    private StarterAssets.ThirdPersonController playerController;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        playerController = GetComponent<StarterAssets.ThirdPersonController>();

        // Lägg till AudioSource om det saknas
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // Initiera hälsa och hunger till max
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        lastDamageTime = -damageImmunityTime; // Se till att spelaren inte är immun från början

        // Meddela UI om initiala värden
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    private void Update()
    {
        if (isDead)
            return;

        // Hantera hunger över tid om hungersystemet är aktiverat
        if (enableHungerSystem)
        {
            HandleHunger();
        }
    }

    private void HandleHunger()
    {
        float currentHungerRate = hungerDecreaseRate;

        // Öka hungerminskning vid sprinting
        if (playerController != null && playerController.Grounded)
        {
            // Kolla om spelaren springer (antar att hastigheten är över gånghastigheten)
            if (playerController.GetComponent<CharacterController>().velocity.magnitude > playerController.MoveSpeed + 0.1f)
            {
                currentHungerRate *= hungerDecreaseMultiplier;
            }
        }

        // Minska hunger över tid
        currentHunger -= currentHungerRate * Time.deltaTime;

        // Kontrollera om hungern når 0 (död av svält)
        if (currentHunger <= 0)
        {
            currentHunger = 0;
            Die(DeathReason.Hunger);
            return;
        }

        // Påverka spelarens sprintförmåga baserat på hunger
        UpdatePlayerStaminaBasedOnHunger();

        // Meddela UI om hunger har ändrats
        OnHungerChanged?.Invoke(currentHunger, maxHunger);

        // Spela hungersljud periodvis när hungern är låg
        if (currentHunger <= 10f)
        {
            hungerSoundTimer -= Time.deltaTime;
            if (hungerSoundTimer <= 0f && hungerSound != null)
            {
                audioSource.PlayOneShot(hungerSound);
                hungerSoundTimer = Random.Range(5f, 15f); // Slumpmässig tid tills nästa ljud
            }
        }
    }

    private void UpdatePlayerStaminaBasedOnHunger()
    {
        if (playerController != null)
        {
            // Beräkna en hungerfaktor mellan 0 och 1
            float hungerFactor = Mathf.Clamp01(currentHunger / maxHunger);

            // Påverka spelarens sprintförmåga baserat på hunger
            // Detta kan implementeras med en sprintmultiplikator i ThirdPersonController
            // Exempel: playerController.SprintSpeedMultiplier = Mathf.Lerp(0.5f, 1.0f, hungerFactor);
        }
    }

    // Typ av skada för att hantera olika skador olika
    public enum DamageType
    {
        Combat,
        Fall,
        Hunger,
        Environment,
        Poison
    }

    public void TakeDamage(float damage, DamageType damageType = DamageType.Combat)
    {
        if (isDead)
            return;

        // Immunitetscheck för stridsskada (inte för fallskada eller hungerskada)
        if (damageType == DamageType.Combat && Time.time - lastDamageTime < damageImmunityTime)
        {
            return;
        }

        // Applicera skada (med multiplikator för stridsskada)
        if (damageType == DamageType.Combat)
        {
            damage *= combatDamageMultiplier;
        }

        currentHealth -= damage;
        lastDamageTime = Time.time;

        // Olika effekter baserat på skadetyp
        switch (damageType)
        {
            case DamageType.Combat:
                if (damageSound != null && damage > 1f)
                {
                    audioSource.PlayOneShot(damageSound);
                }
                break;
            case DamageType.Fall:
                if (fallDamageSound != null)
                {
                    audioSource.PlayOneShot(fallDamageSound);
                }
                break;
            default:
                if (damageSound != null && damage > 1f)
                {
                    audioSource.PlayOneShot(damageSound);
                }
                break;
        }

        // Begränsa hälsan till 0 som minimum
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        // Meddela UI om hälsa har ändrats
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Kontrollera om spelaren har dött (hälsa = 0)
        if (currentHealth <= 0)
        {
            DeathReason reason = (damageType == DamageType.Fall) ? DeathReason.Fall : DeathReason.Health;
            Die(reason);
        }
    }

    public void Eat(float foodValue)
    {
        if (isDead)
            return;

        // Lägg till hunger
        currentHunger += foodValue;
        currentHunger = Mathf.Min(currentHunger, maxHunger);

        // Spela ljud
        if (eatSound != null)
        {
            audioSource.PlayOneShot(eatSound);
        }

        // Meddela UI om hunger har ändrats
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void CheckFallDamage(float impactVelocity)
    {
        // Skippa fallskadekontroll om fallskada är inaktiverad
        if (!enableFallDamage)
            return;

        // Om hastigheten är negativ (faller) och över tröskelvärdet
        if (impactVelocity < -fallDamageThreshold)
        {
            // Beräkna skadans storlek baserat på fallhastighet
            float rawDamage = Mathf.Abs(impactVelocity + fallDamageThreshold) * fallDamageMultiplier;

            // Begränsa skadan till min/max-värdena
            float damage = Mathf.Clamp(rawDamage, minFallDamage, maxFallDamage);

            // Applicera skadan
            TakeDamage(damage, DamageType.Fall);

            Debug.Log($"Fallskada: {damage} från hastighet: {impactVelocity}");
        }
    }

    private void Die(DeathReason reason)
    {
        if (isDead)
            return;

        isDead = true;

        // Spela ljud
        if (deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        string deathMessage = "";
        switch (reason)
        {
            case DeathReason.Health:
                deathMessage = "Spelaren dog av skador!";
                break;
            case DeathReason.Hunger:
                deathMessage = "Spelaren svalt ihjäl!";
                break;
            case DeathReason.Fall:
                deathMessage = "Spelaren dog av fallskada!";
                break;
        }

        Debug.Log(deathMessage);

        // Inaktivera spelarens rörelse
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Inaktivera CharacterController för att stoppa rörelser
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Meddela UI om spelardöd
        OnPlayerDeath?.Invoke(reason);

        // För enkelhets skull, laddar vi om scenen efter en fördröjning
        StartCoroutine(ReloadSceneAfterDelay(3f));
    }

    private IEnumerator ReloadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void RestoreHealth(float amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        // Meddela UI om hälsa har ändrats
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Returnerar aktuell hunger som en procentandel (0-1)
    public float GetHungerPercentage()
    {
        return currentHunger / maxHunger;
    }

    // Returnerar aktuell hälsa som en procentandel (0-1)
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}