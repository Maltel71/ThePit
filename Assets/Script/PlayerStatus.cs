using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    // Event f�r UI-uppdateringar
    public delegate void HealthChangedDelegate(float currentHealth, float maxHealth);
    public event HealthChangedDelegate OnHealthChanged;

    public delegate void HungerChangedDelegate(float currentHunger, float maxHunger);
    public event HungerChangedDelegate OnHungerChanged;

    public delegate void PlayerDeathDelegate(DeathReason reason);
    public event PlayerDeathDelegate OnPlayerDeath;

    [Header("H�lsa")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Fallskada")]
    [SerializeField] private bool enableFallDamage = true;
    [SerializeField] private float fallDamageThreshold = 10f; // Hastighet som kr�vs f�r att ta fallskada
    [SerializeField] private float fallDamageMultiplier = 5f; // Hur mycket fallskada som tas per hastighets�kning
    [SerializeField] private float minFallDamage = 5f; // Minsta m�jliga fallskada
    [SerializeField] private float maxFallDamage = 50f; // H�gsta m�jliga fallskada

    [Header("Stridsskada")]
    [SerializeField] private float combatDamageMultiplier = 1.0f; // Multiplikator f�r stridsskada (f�r sv�righetsgrad)
    [SerializeField] private float damageImmunityTime = 0.5f; // Tid efter att ha tagit skada d� spelaren �r immun (f�r att undvika flera skador i f�ljd)
    private float lastDamageTime; // Senaste tidpunkt d� spelaren tog skada

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

    // D�dsorsaker
    public enum DeathReason
    {
        Health, // D�d av skada
        Hunger, // D�d av sv�lt
        Fall    // D�d av fallskada
    }

    // Publika properties f�r UI-information
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
    private float hungerSoundTimer = 0f; // Timer f�r att inte spela hungersljud f�r ofta

    // Referens till ThirdPersonController f�r att p�verka r�relsehastighet baserat p� hunger
    private StarterAssets.ThirdPersonController playerController;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        playerController = GetComponent<StarterAssets.ThirdPersonController>();

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
        lastDamageTime = -damageImmunityTime; // Se till att spelaren inte �r immun fr�n b�rjan

        // Meddela UI om initiala v�rden
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    private void Update()
    {
        if (isDead)
            return;

        // Hantera hunger �ver tid om hungersystemet �r aktiverat
        if (enableHungerSystem)
        {
            HandleHunger();
        }
    }

    private void HandleHunger()
    {
        float currentHungerRate = hungerDecreaseRate;

        // �ka hungerminskning vid sprinting
        if (playerController != null && playerController.Grounded)
        {
            // Kolla om spelaren springer (antar att hastigheten �r �ver g�nghastigheten)
            if (playerController.GetComponent<CharacterController>().velocity.magnitude > playerController.MoveSpeed + 0.1f)
            {
                currentHungerRate *= hungerDecreaseMultiplier;
            }
        }

        // Minska hunger �ver tid
        currentHunger -= currentHungerRate * Time.deltaTime;

        // Kontrollera om hungern n�r 0 (d�d av sv�lt)
        if (currentHunger <= 0)
        {
            currentHunger = 0;
            Die(DeathReason.Hunger);
            return;
        }

        // P�verka spelarens sprintf�rm�ga baserat p� hunger
        UpdatePlayerStaminaBasedOnHunger();

        // Meddela UI om hunger har �ndrats
        OnHungerChanged?.Invoke(currentHunger, maxHunger);

        // Spela hungersljud periodvis n�r hungern �r l�g
        if (currentHunger <= 10f)
        {
            hungerSoundTimer -= Time.deltaTime;
            if (hungerSoundTimer <= 0f && hungerSound != null)
            {
                audioSource.PlayOneShot(hungerSound);
                hungerSoundTimer = Random.Range(5f, 15f); // Slumpm�ssig tid tills n�sta ljud
            }
        }
    }

    private void UpdatePlayerStaminaBasedOnHunger()
    {
        if (playerController != null)
        {
            // Ber�kna en hungerfaktor mellan 0 och 1
            float hungerFactor = Mathf.Clamp01(currentHunger / maxHunger);

            // P�verka spelarens sprintf�rm�ga baserat p� hunger
            // Detta kan implementeras med en sprintmultiplikator i ThirdPersonController
            // Exempel: playerController.SprintSpeedMultiplier = Mathf.Lerp(0.5f, 1.0f, hungerFactor);
        }
    }

    // Typ av skada f�r att hantera olika skador olika
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

        // Immunitetscheck f�r stridsskada (inte f�r fallskada eller hungerskada)
        if (damageType == DamageType.Combat && Time.time - lastDamageTime < damageImmunityTime)
        {
            return;
        }

        // Applicera skada (med multiplikator f�r stridsskada)
        if (damageType == DamageType.Combat)
        {
            damage *= combatDamageMultiplier;
        }

        currentHealth -= damage;
        lastDamageTime = Time.time;

        // Olika effekter baserat p� skadetyp
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

        // Begr�nsa h�lsan till 0 som minimum
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        // Meddela UI om h�lsa har �ndrats
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Kontrollera om spelaren har d�tt (h�lsa = 0)
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

        // L�gg till hunger
        currentHunger += foodValue;
        currentHunger = Mathf.Min(currentHunger, maxHunger);

        // Spela ljud
        if (eatSound != null)
        {
            audioSource.PlayOneShot(eatSound);
        }

        // Meddela UI om hunger har �ndrats
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void CheckFallDamage(float impactVelocity)
    {
        // Skippa fallskadekontroll om fallskada �r inaktiverad
        if (!enableFallDamage)
            return;

        // Om hastigheten �r negativ (faller) och �ver tr�skelv�rdet
        if (impactVelocity < -fallDamageThreshold)
        {
            // Ber�kna skadans storlek baserat p� fallhastighet
            float rawDamage = Mathf.Abs(impactVelocity + fallDamageThreshold) * fallDamageMultiplier;

            // Begr�nsa skadan till min/max-v�rdena
            float damage = Mathf.Clamp(rawDamage, minFallDamage, maxFallDamage);

            // Applicera skadan
            TakeDamage(damage, DamageType.Fall);

            Debug.Log($"Fallskada: {damage} fr�n hastighet: {impactVelocity}");
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
                deathMessage = "Spelaren svalt ihj�l!";
                break;
            case DeathReason.Fall:
                deathMessage = "Spelaren dog av fallskada!";
                break;
        }

        Debug.Log(deathMessage);

        // Inaktivera spelarens r�relse
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Inaktivera CharacterController f�r att stoppa r�relser
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Meddela UI om spelard�d
        OnPlayerDeath?.Invoke(reason);

        // F�r enkelhets skull, laddar vi om scenen efter en f�rdr�jning
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

        // Meddela UI om h�lsa har �ndrats
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Returnerar aktuell hunger som en procentandel (0-1)
    public float GetHungerPercentage()
    {
        return currentHunger / maxHunger;
    }

    // Returnerar aktuell h�lsa som en procentandel (0-1)
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}