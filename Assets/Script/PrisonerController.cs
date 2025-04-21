using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Kontrollerar beteendet f�r en f�nge i spelet. F�ngar vandrar slumpm�ssigt, letar efter mat,
/// reagerar p� hot genom att fly, och interagerar med matplattformen.
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
    [SerializeField] private float awarenessRange = 8f; // Hur l�ngt f�ngen kan uppt�cka saker
    [SerializeField] private float moveSpeed = 2f; // Normal r�relsehastighet
    [SerializeField] private float runSpeed = 3.5f; // R�relsehastighet n�r hungrig/mat uppt�cks
    [SerializeField] private float wanderRadius = 8f; // Radie f�r slumpm�ssig vandring
    [SerializeField] private float minIdleTime = 2f; // Min v�ntetid mellan vandringar
    [SerializeField] private float maxIdleTime = 7f; // Max v�ntetid mellan vandringar
    [SerializeField] private float fleeDistance = 6f; // Avst�nd f�ngen flyr vid fara
    [SerializeField] private float platformDetectionRadius = 5f; // Radie f�r att uppt�cka matplattform

    [Header("Floor Detection")]
    [SerializeField] private Transform floorCheck; // Punkt f�r att detektera vilken v�ning f�ngen �r p�
    [SerializeField] private LayerMask floorLayer; // Layer f�r v�ningar

    [Header("Ljud")]
    [SerializeField] private AudioClip[] idleSounds; // Slumpm�ssiga ljud vid vandring
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

    // Tillst�nd f�r AI
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

        // S�tt startposition
        startPosition = transform.position;
        currentWanderTarget = startPosition;

        // Hitta spelaren
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Start()
    {
        // Initiera h�lsa och hunger
        currentHealth = maxHealth;
        currentHunger = Random.Range(maxHunger * 0.3f, maxHunger * 0.8f); // B�rja med viss hunger

        // Konfigurera NavMeshAgent
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.5f;
            agent.angularSpeed = 120;
        }
        else
        {
            Debug.LogError("NavMeshAgent saknas p� f�ngen!");
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

        // Kontrollera om det finns hot/mat i n�rheten och uppdatera tillst�nd
        UpdateAIState();

        // Hantera idleljud
        HandleIdleSounds();
    }

    private void UpdateHunger()
    {
        // Minska hunger �ver tid
        currentHunger -= hungerDecreaseRate * Time.deltaTime;

        // Begr�nsa minimum till 0
        if (currentHunger < 0)
            currentHunger = 0;

        // Om hunger �r v�ldigt l�g, ta skada
        if (currentHunger <= 5f)
        {
            TakeDamage(0.2f * Time.deltaTime);
        }
    }

    private void UpdateAIState()
    {
        // H�g prioritet: Om det finns hot i n�rheten, fly
        if (IsEnemyNearby())
        {
            // Avbryt v�ntan om f�ngen flyr
            isIdle = false;
            currentState = PrisonerState.Flee;
            return;
        }

        // Om plattformen med mat �r h�r, prioritera det
        if (IsFoodPlatformNearby())
        {
            isIdle = false;
            currentState = PrisonerState.WaitForPlatform;
            return;
        }

        // Om mat finns i n�rheten och �r hungrig, g� till maten
        if (currentHunger < 70f && IsFoodNearby())
        {
            isIdle = false;
            currentState = PrisonerState.GoToFood;
            return;
        }

        // Annars, forts�tt med nuvarande tillst�nd (idle eller wander)
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
            // Hantera olika tillst�nd
            switch (currentState)
            {
                case PrisonerState.Idle:
                    // Stanna och v�nta en stund
                    if (agent.hasPath)
                    {
                        agent.ResetPath();
                    }

                    if (animator != null)
                    {
                        animator.SetFloat("Speed", 0);
                        animator.SetBool("IsIdle", true);
                    }

                    // V�nta slumpm�ssig tid
                    float waitTime = Random.Range(minIdleTime, maxIdleTime);
                    yield return new WaitForSeconds(waitTime);

                    // Efter v�ntan, b�rja vandra
                    isIdle = true;
                    break;

                case PrisonerState.Wander:
                    // V�lj slumpm�ssig destination inom vandringsradien
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

                    // V�nta tills destinationen n�s eller en viss tid passerar
                    float timeout = 0f;
                    while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    {
                        timeout += Time.deltaTime;
                        if (timeout > 10f || currentState != PrisonerState.Wander) // Avbryt om det tar f�r l�ng tid eller tillst�ndet �ndras
                        {
                            break;
                        }
                        yield return null;
                    }

                    // Efter vandring, bli idle igen
                    isIdle = false;
                    break;

                case PrisonerState.GoToFood:
                    // Hitta n�rmaste mat och g� till den
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

                        // V�nta tills n�ra maten eller tillst�ndet �ndras
                        timeout = 0f;
                        while (Vector3.Distance(transform.position, nearestFood.transform.position) > 1.5f &&
                               currentState == PrisonerState.GoToFood)
                        {
                            timeout += Time.deltaTime;
                            if (timeout > 15f) // Avbryt om det tar f�r l�ng tid
                            {
                                break;
                            }
                            yield return null;
                        }

                        // Om n�ra maten och tillst�ndet fortfarande �r GoToFood, �t den
                        if (currentState == PrisonerState.GoToFood &&
                            Vector3.Distance(transform.position, nearestFood.transform.position) <= 1.5f)
                        {
                            // Simulera att �ta maten
                            Eat(20f);

                            if (animator != null)
                            {
                                animator.SetTrigger("Eat");
                            }

                            yield return new WaitForSeconds(1.5f); // Tid att �ta
                        }
                    }

                    // Efter att ha �tit eller om ingen mat hittades, �terg� till idle
                    isIdle = false;
                    break;

                case PrisonerState.Flee:
                    // Hitta fiende/hot att fly fr�n
                    Transform threatSource = FindNearestThreat();
                    if (threatSource != null)
                    {
                        // Ber�kna flyktdestination (bort fr�n hotet)
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

                            // V�nta tills destinationen n�s eller en viss tid passerar
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

                    // Efter flykt, �terg� till idle
                    isIdle = false;
                    break;

                case PrisonerState.WaitForPlatform:
                    // G� till plattformsomr�det och v�nta p� mat
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

                        // V�nta tills n�ra plattformen eller tillst�ndet �ndras
                        timeout = 0f;
                        while (Vector3.Distance(transform.position, platform.position) > 2.0f &&
                               currentState == PrisonerState.WaitForPlatform)
                        {
                            timeout += Time.deltaTime;
                            if (timeout > 15f) // Avbryt om det tar f�r l�ng tid
                            {
                                break;
                            }
                            yield return null;
                        }

                        // N�r plattformen �r n�dd, v�nta p� mat
                        if (currentState == PrisonerState.WaitForPlatform)
                        {
                            // Titta mot plattformen
                            Vector3 lookDir = platform.position - transform.position;
                            lookDir.y = 0;
                            if (lookDir != Vector3.zero)
                            {
                                transform.rotation = Quaternion.LookRotation(lookDir);
                            }

                            // V�nta p� att mat ska dyka upp
                            yield return new WaitForSeconds(Random.Range(3f, 6f));

                            // Kolla efter mat igen och f�rs�k �ta om det finns
                            FoodItem foodOnPlatform = FindNearestFood();
                            if (foodOnPlatform != null &&
                                Vector3.Distance(transform.position, foodOnPlatform.transform.position) < 3.0f)
                            {
                                // G� till maten
                                agent.SetDestination(foodOnPlatform.transform.position);

                                // V�nta tills n�ra maten
                                timeout = 0f;
                                while (Vector3.Distance(transform.position, foodOnPlatform.transform.position) > 1.5f)
                                {
                                    timeout += Time.deltaTime;
                                    if (timeout > 5f) // Avbryt om det tar f�r l�ng tid
                                    {
                                        break;
                                    }
                                    yield return null;
                                }

                                // �t maten
                                Eat(30f);

                                if (animator != null)
                                {
                                    animator.SetTrigger("Eat");
                                }

                                yield return new WaitForSeconds(2f);
                            }
                        }
                    }

                    // Efter att ha v�ntat p� plattformen, �terg� till vandring
                    isIdle = true;
                    break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void HandleIdleSounds()
    {
        // Spela slumpm�ssiga idleljud n�r f�ngen inte flyr
        if (currentState != PrisonerState.Flee)
        {
            idleSoundTimer -= Time.deltaTime;
            if (idleSoundTimer <= 0 && idleSounds != null && idleSounds.Length > 0)
            {
                // V�lj ett slumpm�ssigt ljud
                int soundIndex = Random.Range(0, idleSounds.Length);
                if (idleSounds[soundIndex] != null)
                {
                    audioSource.PlayOneShot(idleSounds[soundIndex]);
                }

                // �terst�ll timer med slumpm�ssig variation
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

        // Byt till flykttillst�nd om man tar skada
        currentState = PrisonerState.Flee;
        isIdle = false;

        // Om h�lsan n�r 0, d�
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

        // Spela d�d-animation om s�dan finns
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

        // F�rst�r objekt efter en viss tid
        Destroy(gameObject, 10f);
    }

    public void Eat(float foodValue)
    {
        // �terst�ll hunger
        currentHunger += foodValue;
        if (currentHunger > maxHunger)
        {
            currentHunger = maxHunger;
        }

        // Spela �tljud
        if (eatSound != null)
        {
            audioSource.PlayOneShot(eatSound);
        }
    }

    private Transform FindNearestThreat()
    {
        // S�k efter hot (spelaren eller fiender) inom medvetenhetsradien
        Collider[] colliders = Physics.OverlapSphere(transform.position, awarenessRange);

        Transform nearestThreat = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            // Kolla om det �r spelaren eller en fiende
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
        // Kolla om n�gon fiende eller spelaren �r inom medvetenhetsradien
        return FindNearestThreat() != null;
    }

    private FoodItem FindNearestFood()
    {
        // S�k efter objekt med FoodItem-skript inom medvetenhetsradien
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
        // Du beh�ver anpassa detta baserat p� hur din matplattform �r implementerad
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
            // Eftersom FoodItem redan har kollisionslogik, kommer den att hantera �t-logiken
            // Men vi kan ocks� manuellt �terst�lla hunger h�r om det beh�vs
        }
    }

    // F�r visuell debugging
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