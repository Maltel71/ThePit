using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Kontrollerar beteendet för en fånge i spelet. Fångar vandrar slumpmässigt, letar efter mat,
/// reagerar på hot genom att fly, och interagerar med matplattformen.
/// </summary>
public class PrisonerController : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private float maxHealth = 80f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger;
    [SerializeField] private float hungerDecreaseRate = 0.2f;

    [Header("Beteende")]
    [SerializeField] private float awarenessRange = 8f; // Hur långt fången kan upptäcka saker
    [SerializeField] private float moveSpeed = 2f; // Normal rörelsehastighet
    [SerializeField] private float runSpeed = 3.5f; // Rörelsehastighet när hungrig/mat upptäcks
    [SerializeField] private float wanderRadius = 8f; // Radie för slumpmässig vandring
    [SerializeField] private float minIdleTime = 2f; // Min väntetid mellan vandringar
    [SerializeField] private float maxIdleTime = 7f; // Max väntetid mellan vandringar
    [SerializeField] private float fleeDistance = 6f; // Avstånd fången flyr vid fara
    [SerializeField] private float platformDetectionRadius = 5f; // Radie för att upptäcka matplattform

    [Header("Floor Detection")]
    [SerializeField] private Transform floorCheck; // Punkt för att detektera vilken våning fången är på
    [SerializeField] private LayerMask floorLayer; // Layer för våningar

    [Header("Ljud")]
    [SerializeField] private AudioClip[] idleSounds; // Slumpmässiga ljud vid vandring
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip eatSound;
    [SerializeField] private AudioClip fleeSound;

    // Privata variabler
    private NavMeshAgent agent;
    private Transform player;
    private bool isIdle = false;
    private bool isDead = false;
    private AudioSource audioSource;
    private Animator animator;
    private Vector3 startPosition;
    private Vector3 currentWanderTarget;
    private float idleSoundTimer;
    private float idleSoundInterval = 15f; // Hur ofta idleljud spelas

    // Tillstånd för AI
    private enum PrisonerState
    {
        Idle,
        Wander,
        GoToFood,
        Flee,
        WaitForPlatform
    }
    private PrisonerState currentState = PrisonerState.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Sätt startposition
        startPosition = transform.position;
        currentWanderTarget = startPosition;

        // Hitta spelaren
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Start()
    {
        // Initiera hälsa och hunger
        currentHealth = maxHealth;
        currentHunger = Random.Range(maxHunger * 0.3f, maxHunger * 0.8f); // Börja med viss hunger

        // Konfigurera NavMeshAgent
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.5f;
            agent.angularSpeed = 120;
        }
        else
        {
            Debug.LogError("NavMeshAgent saknas på fången!");
        }

        // Starta beteenderutinen
        StartCoroutine(PrisonerBehaviorRoutine());

        // Initiera idleljudstimer
        idleSoundTimer = Random.Range(idleSoundInterval * 0.5f, idleSoundInterval * 1.5f);
    }

    private void Update()
    {
        if (isDead)
            return;

        // Uppdatera hunger
        UpdateHunger();

        // Kontrollera om det finns hot/mat i närheten och uppdatera tillstånd
        UpdateAIState();

        // Hantera idleljud
        HandleIdleSounds();
    }

    private void UpdateHunger()
    {
        // Minska hunger över tid
        currentHunger -= hungerDecreaseRate * Time.deltaTime;

        // Begränsa minimum till 0
        if (currentHunger < 0)
            currentHunger = 0;

        // Om hunger är väldigt låg, ta skada
        if (currentHunger <= 5f)
        {
            TakeDamage(0.2f * Time.deltaTime);
        }
    }

    private void UpdateAIState()
    {
        // Hög prioritet: Om det finns hot i närheten, fly
        if (IsEnemyNearby())
        {
            // Avbryt väntan om fången flyr
            isIdle = false;
            currentState = PrisonerState.Flee;
            return;
        }

        // Om plattformen med mat är här, prioritera det
        if (IsFoodPlatformNearby())
        {
            isIdle = false;
            currentState = PrisonerState.WaitForPlatform;
            return;
        }

        // Om mat finns i närheten och är hungrig, gå till maten
        if (currentHunger < 70f && IsFoodNearby())
        {
            isIdle = false;
            currentState = PrisonerState.GoToFood;
            return;
        }

        // Annars, fortsätt med nuvarande tillstånd (idle eller wander)
        if (!isIdle && (currentState == PrisonerState.Idle || agent.remainingDistance < 0.5f))
        {
            currentState = PrisonerState.Idle;
        }
        else if (isIdle)
        {
            currentState = PrisonerState.Wander;
        }
    }

    private IEnumerator PrisonerBehaviorRoutine()
    {
        while (!isDead)
        {
            // Hantera olika tillstånd
            switch (currentState)
            {
                case PrisonerState.Idle:
                    // Stanna och vänta en stund
                    if (agent.hasPath)
                    {
                        agent.ResetPath();
                    }

                    if (animator != null)
                    {
                        animator.SetFloat("Speed", 0);
                        animator.SetBool("IsIdle", true);
                    }

                    // Vänta slumpmässig tid
                    float waitTime = Random.Range(minIdleTime, maxIdleTime);
                    yield return new WaitForSeconds(waitTime);

                    // Efter väntan, börja vandra
                    isIdle = true;
                    break;

                case PrisonerState.Wander:
                    // Välj slumpmässig destination inom vandringsradien
                    Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                    randomDirection.y = 0;
                    Vector3 destination = startPosition + randomDirection;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(destination, out hit, wanderRadius, NavMesh.AllAreas))
                    {
                        agent.speed = moveSpeed;
                        agent.SetDestination(hit.position);

                        if (animator != null)
                        {
                            animator.SetFloat("Speed", 0.5f);
                            animator.SetBool("IsIdle", false);
                        }
                    }

                    // Vänta tills destinationen nås eller en viss tid passerar
                    float timeout = 0f;
                    while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    {
                        timeout += Time.deltaTime;
                        if (timeout > 10f || currentState != PrisonerState.Wander) // Avbryt om det tar för lång tid eller tillståndet ändras
                        {
                            break;
                        }
                        yield return null;
                    }

                    // Efter vandring, bli idle igen
                    isIdle = false;
                    break;

                case PrisonerState.GoToFood:
                    // Hitta närmaste mat och gå till den
                    FoodItem nearestFood = FindNearestFood();
                    if (nearestFood != null)
                    {
                        agent.speed = runSpeed;
                        agent.SetDestination(nearestFood.transform.position);

                        if (animator != null)
                        {
                            animator.SetFloat("Speed", 0.8f);
                            animator.SetBool("IsIdle", false);
                        }

                        // Vänta tills nära maten eller tillståndet ändras
                        timeout = 0f;
                        while (Vector3.Distance(transform.position, nearestFood.transform.position) > 1.5f &&
                               currentState == PrisonerState.GoToFood)
                        {
                            timeout += Time.deltaTime;
                            if (timeout > 15f) // Avbryt om det tar för lång tid
                            {
                                break;
                            }
                            yield return null;
                        }

                        // Om nära maten och tillståndet fortfarande är GoToFood, ät den
                        if (currentState == PrisonerState.GoToFood &&
                            Vector3.Distance(transform.position, nearestFood.transform.position) <= 1.5f)
                        {
                            // Simulera att äta maten
                            Eat(20f);

                            if (animator != null)
                            {
                                animator.SetTrigger("Eat");
                            }

                            yield return new WaitForSeconds(1.5f); // Tid att äta
                        }
                    }

                    // Efter att ha ätit eller om ingen mat hittades, återgå till idle
                    isIdle = false;
                    break;

                case PrisonerState.Flee:
                    // Hitta fiende/hot att fly från
                    Transform threatSource = FindNearestThreat();
                    if (threatSource != null)
                    {
                        // Beräkna flyktdestination (bort från hotet)
                        Vector3 fleeDirection = (transform.position - threatSource.position).normalized;
                        Vector3 fleeDestination = transform.position + fleeDirection * fleeDistance;

                        NavMeshHit fleeHit;
                        if (NavMesh.SamplePosition(fleeDestination, out fleeHit, fleeDistance, NavMesh.AllAreas))
                        {
                            agent.speed = runSpeed;
                            agent.SetDestination(fleeHit.position);

                            if (animator != null)
                            {
                                animator.SetFloat("Speed", 1.0f);
                                animator.SetBool("IsIdle", false);
                                animator.SetBool("IsFleeing", true);
                            }

                            // Spela flyktljud
                            if (fleeSound != null)
                            {
                                audioSource.PlayOneShot(fleeSound);
                            }

                            // Vänta tills destinationen nås eller en viss tid passerar
                            timeout = 0f;
                            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                            {
                                timeout += Time.deltaTime;
                                if (timeout > 7f || currentState != PrisonerState.Flee)
                                {
                                    break;
                                }
                                yield return null;
                            }

                            if (animator != null)
                            {
                                animator.SetBool("IsFleeing", false);
                            }
                        }
                    }

                    // Efter flykt, återgå till idle
                    isIdle = false;
                    break;

                case PrisonerState.WaitForPlatform:
                    // Gå till plattformsområdet och vänta på mat
                    Transform platform = FindFoodPlatform();
                    if (platform != null)
                    {
                        agent.speed = runSpeed;
                        agent.SetDestination(platform.position);

                        if (animator != null)
                        {
                            animator.SetFloat("Speed", 0.8f);
                            animator.SetBool("IsIdle", false);
                        }

                        // Vänta tills nära plattformen eller tillståndet ändras
                        timeout = 0f;
                        while (Vector3.Distance(transform.position, platform.position) > 2.0f &&
                               currentState == PrisonerState.WaitForPlatform)
                        {
                            timeout += Time.deltaTime;
                            if (timeout > 15f) // Avbryt om det tar för lång tid
                            {
                                break;
                            }
                            yield return null;
                        }

                        // När plattformen är nådd, vänta på mat
                        if (currentState == PrisonerState.WaitForPlatform)
                        {
                            // Titta mot plattformen
                            Vector3 lookDir = platform.position - transform.position;
                            lookDir.y = 0;
                            if (lookDir != Vector3.zero)
                            {
                                transform.rotation = Quaternion.LookRotation(lookDir);
                            }

                            // Vänta på att mat ska dyka upp
                            yield return new WaitForSeconds(Random.Range(3f, 6f));

                            // Kolla efter mat igen och försök äta om det finns
                            FoodItem foodOnPlatform = FindNearestFood();
                            if (foodOnPlatform != null &&
                                Vector3.Distance(transform.position, foodOnPlatform.transform.position) < 3.0f)
                            {
                                // Gå till maten
                                agent.SetDestination(foodOnPlatform.transform.position);

                                // Vänta tills nära maten
                                timeout = 0f;
                                while (Vector3.Distance(transform.position, foodOnPlatform.transform.position) > 1.5f)
                                {
                                    timeout += Time.deltaTime;
                                    if (timeout > 5f) // Avbryt om det tar för lång tid
                                    {
                                        break;
                                    }
                                    yield return null;
                                }

                                // Ät maten
                                Eat(30f);

                                if (animator != null)
                                {
                                    animator.SetTrigger("Eat");
                                }

                                yield return new WaitForSeconds(2f);
                            }
                        }
                    }

                    // Efter att ha väntat på plattformen, återgå till vandring
                    isIdle = true;
                    break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void HandleIdleSounds()
    {
        // Spela slumpmässiga idleljud när fången inte flyr
        if (currentState != PrisonerState.Flee)
        {
            idleSoundTimer -= Time.deltaTime;
            if (idleSoundTimer <= 0 && idleSounds != null && idleSounds.Length > 0)
            {
                // Välj ett slumpmässigt ljud
                int soundIndex = Random.Range(0, idleSounds.Length);
                if (idleSounds[soundIndex] != null)
                {
                    audioSource.PlayOneShot(idleSounds[soundIndex]);
                }

                // Återställ timer med slumpmässig variation
                idleSoundTimer = Random.Range(idleSoundInterval * 0.8f, idleSoundInterval * 1.2f);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        // Spela skadeljud
        if (hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // Byt till flykttillstånd om man tar skada
        currentState = PrisonerState.Flee;
        isIdle = false;

        // Om hälsan når 0, dö
        if (currentHealth <= 0)
        {
            Die();
        }
        else if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
    }

    private void Die()
    {
        isDead = true;

        // Avaktivera NavMeshAgent
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Spela död-animation om sådan finns
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // Inaktivera Collider
        Collider prisonerCollider = GetComponent<Collider>();
        if (prisonerCollider != null)
        {
            prisonerCollider.enabled = false;
        }

        // Förstör objekt efter en viss tid
        Destroy(gameObject, 10f);
    }

    public void Eat(float foodValue)
    {
        // Återställ hunger
        currentHunger += foodValue;
        if (currentHunger > maxHunger)
        {
            currentHunger = maxHunger;
        }

        // Spela ätljud
        if (eatSound != null)
        {
            audioSource.PlayOneShot(eatSound);
        }
    }

    private Transform FindNearestThreat()
    {
        // Sök efter hot (spelaren eller fiender) inom medvetenhetsradien
        Collider[] colliders = Physics.OverlapSphere(transform.position, awarenessRange);

        Transform nearestThreat = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            // Kolla om det är spelaren eller en fiende
            bool isPlayer = collider.CompareTag("Player");
            bool isEnemy = collider.GetComponent<EnemyController>() != null;

            if (isPlayer || isEnemy)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestThreat = collider.transform;
                }
            }
        }

        return nearestThreat;
    }

    private bool IsEnemyNearby()
    {
        // Kolla om någon fiende eller spelaren är inom medvetenhetsradien
        return FindNearestThreat() != null;
    }

    private FoodItem FindNearestFood()
    {
        // Sök efter objekt med FoodItem-skript inom medvetenhetsradien
        Collider[] colliders = Physics.OverlapSphere(transform.position, awarenessRange);

        FoodItem nearestFood = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            FoodItem foodItem = collider.GetComponent<FoodItem>();
            if (foodItem != null)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFood = foodItem;
                }
            }
        }

        return nearestFood;
    }

    private bool IsFoodNearby()
    {
        // Kolla om mat finns inom medvetenhetsradien
        return FindNearestFood() != null;
    }

    private Transform FindFoodPlatform()
    {
        // Denna metod ska hitta matplattformen
        // Du behöver anpassa detta baserat på hur din matplattform är implementerad
        GameObject platform = GameObject.FindGameObjectWithTag("Platform");
        if (platform != null)
        {
            return platform.transform;
        }

        return null;
    }

    private bool IsFoodPlatformNearby()
    {
        // Kolla om matplattformen finns inom detekteringsradien
        Transform platform = FindFoodPlatform();
        if (platform != null)
        {
            float distance = Vector3.Distance(transform.position, platform.position);
            return distance <= platformDetectionRadius;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kontrollera kollision med mat
        FoodItem foodItem = other.GetComponent<FoodItem>();
        if (foodItem != null && currentHunger < maxHunger)
        {
            // Eftersom FoodItem redan har kollisionslogik, kommer den att hantera ät-logiken
            // Men vi kan också manuellt återställa hunger här om det behövs
        }
    }

    // För visuell debugging
    private void OnDrawGizmosSelected()
    {
        // Rita medvetenhetsradie
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, awarenessRange);

        // Rita vandringsradie
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(startPosition, wanderRadius);

        // Rita plattformsdetektionsradie
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, platformDetectionRadius);
    }
}