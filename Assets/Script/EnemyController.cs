using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger;
    [SerializeField] private float hungerDecreaseRate = 0.3f;
    [SerializeField] private float maxFrustration = 100f; // Ny variabel för frustration
    [SerializeField] private float currentFrustration = 0f; // Ny variabel för frustration
    [SerializeField] private float frustrationIncreaseRate = 0.5f; // Hastighet för frustrationökning
    [SerializeField] private float frustrationDecreaseRate = 0.1f; // Hastighet för frustrationminskning

    [Header("Beteende")]
    [SerializeField] private float detectionRange = 10f; // Hur långt fienden kan upptäcka spelaren
    [SerializeField] private float attackRange = 2f; // Avstånd för att attackera
    [SerializeField] private float attackCooldown = 1.5f; // Tid mellan attacker
    [SerializeField] private float attackDamage = 10f; // Skada per attack
    [SerializeField] private float moveSpeed = 3.5f; // Rörelsehastighet vid jakt
    [SerializeField] private float patrolSpeed = 1.5f; // Rörelsehastighet vid patrullering
    [SerializeField] private float minPatrolWaitTime = 1f; // Min väntetid mellan patrullpunkter
    [SerializeField] private float maxPatrolWaitTime = 4f; // Max väntetid mellan patrullpunkter
    [SerializeField] private float patrolRadius = 10f; // Maximal radie för patrullpunkter
    [SerializeField] private float frustrationThreshold = 80f; // Tröskelvärde för när frustration påverkar beteende

    [Header("Floor Detection")]
    [SerializeField] private Transform floorCheck; // Punkt för att detektera vilken våning fienden är på
    [SerializeField] private LayerMask floorLayer; // Layer för våningar

    [Header("Ljud")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip frustratedSound; // Nytt ljud för när fienden är frustrerad

    // Privata variabler
    private NavMeshAgent agent;
    private Transform player;
    private bool isPlayerInRange = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private int currentFloorIndex = -1;
    private AudioSource audioSource;
    private Animator animator;
    private bool isWaitingAtPatrolPoint = false;
    private Vector3 startPosition;
    private Vector3 currentPatrolTarget;
    private float frustrationSoundTimer = 0f; // Timer för frustrations-ljud

    // Tillstånd för AI
    private enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Eat,
        Hungry,
        Frustrated // Nytt tillstånd för frustration
    }
    private EnemyState currentState = EnemyState.Patrol;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Sätt startposition för patrullering
        startPosition = transform.position;
        currentPatrolTarget = startPosition;

        // Hitta spelaren
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Start()
    {
        // Initiera hälsa och hunger
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentFrustration = 0f; // Initiera frustration till 0

        // Konfigurera NavMeshAgent
        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.stoppingDistance = attackRange * 0.8f;
        }
        else
        {
            Debug.LogError("NavMeshAgent saknas på fienden!");
        }

        // Starta patrulleringsrutinen
        StartCoroutine(PatrolRoutine());
    }

    private void Update()
    {
        if (isDead || player == null)
            return;

        // Uppdatera hunger
        UpdateHunger();

        // Uppdatera frustration
        UpdateFrustration();

        // Kontrollera om spelaren är inom detekteringsavstånd
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isPlayerInRange = distanceToPlayer <= detectionRange;

        // Uppdatera AI-tillstånd baserat på situation
        UpdateAIState(distanceToPlayer);

        // Utför handlingar baserat på aktuellt tillstånd
        PerformStateAction();
    }

    private void UpdateHunger()
    {
        // Minska hunger över tid
        currentHunger -= hungerDecreaseRate * Time.deltaTime;

        // Begränsa minimum till 0
        if (currentHunger < 0)
            currentHunger = 0;

        // Om hunger är väldigt låg, ta skada och öka frustration
        if (currentHunger <= 5f)
        {
            TakeDamage(0.5f * Time.deltaTime);
            // Öka frustration snabbare när hungrig
            currentFrustration += frustrationIncreaseRate * 2f * Time.deltaTime;
        }
        else if (currentHunger <= 30f)
        {
            // Öka frustration när hungrig
            currentFrustration += frustrationIncreaseRate * Time.deltaTime;
        }
    }

    private void UpdateFrustration()
    {
        // Hantera frustrationsnivå baserat på situation

        // Minska frustration över tid om fienden är mätt
        if (currentHunger > 60f)
        {
            currentFrustration -= frustrationDecreaseRate * Time.deltaTime;
        }

        // Öka frustration om fienden ser spelaren men inte kan nå den
        if (isPlayerInRange && !CheckIfOnSameFloor())
        {
            currentFrustration += frustrationIncreaseRate * 0.5f * Time.deltaTime;
        }

        // Håll frustration inom gränser (0-100)
        currentFrustration = Mathf.Clamp(currentFrustration, 0f, maxFrustration);

        // Spela frustrations-ljud periodvis när frustrationen är hög
        if (currentFrustration >= 80f)
        {
            frustrationSoundTimer -= Time.deltaTime;
            if (frustrationSoundTimer <= 0f && frustratedSound != null)
            {
                audioSource.PlayOneShot(frustratedSound);
                frustrationSoundTimer = Random.Range(5f, 10f); // Slumpmässig tid tills nästa ljud
            }
        }
    }

    private void UpdateAIState(float distanceToPlayer)
    {
        // Kontrollera om fienden är på samma våning som spelaren
        bool isOnSameFloor = CheckIfOnSameFloor();

        // Prioritera frustrerat tillstånd om frustrationen är över tröskelvärdet
        if (currentFrustration >= frustrationThreshold)
        {
            currentState = EnemyState.Frustrated;
            return;
        }

        // Prioritera att äta om hungern är låg och mat finns tillgänglig
        if (currentHunger < 30f && IsThereAnyFoodNearby())
        {
            currentState = EnemyState.Eat;
            return;
        }

        // Prioritera att jaga om spelaren är inom räckhåll och på samma våning
        if (isPlayerInRange && isOnSameFloor)
        {
            if (distanceToPlayer <= attackRange && !isAttacking)
            {
                currentState = EnemyState.Attack;
            }
            else
            {
                currentState = EnemyState.Chase;
            }
        }
        else
        {
            // Om fienden är hungrig men inte super-hungrig, leta efter mat
            if (currentHunger < 50f)
            {
                currentState = EnemyState.Hungry;
            }
            else
            {
                currentState = EnemyState.Patrol;
            }
        }
    }

    private void PerformStateAction()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                // Patrulleringslogik hanteras i PatrolRoutine coroutine
                agent.speed = patrolSpeed;
                if (animator != null)
                {
                    animator.SetBool("IsChasing", false);
                    animator.SetBool("IsAttacking", false);
                }
                break;

            case EnemyState.Chase:
                isWaitingAtPatrolPoint = false; // Avbryt patrullväntan om den jagar
                agent.speed = moveSpeed;
                agent.SetDestination(player.position);
                if (animator != null)
                {
                    animator.SetBool("IsChasing", true);
                    animator.SetBool("IsAttacking", false);
                }
                break;

            case EnemyState.Attack:
                agent.SetDestination(transform.position); // Stoppa rörelse vid attack
                AttackPlayer();
                if (animator != null)
                {
                    animator.SetBool("IsChasing", false);
                    animator.SetBool("IsAttacking", true);
                }
                break;

            case EnemyState.Eat:
                // Hitta närmaste mat och gå till den
                GoToNearestFood();
                if (animator != null)
                {
                    animator.SetBool("IsChasing", false);
                    animator.SetBool("IsAttacking", false);
                }
                break;

            case EnemyState.Hungry:
                // Sök efter mat mer aktivt när hungrig
                SearchForFood();
                if (animator != null)
                {
                    animator.SetBool("IsChasing", false);
                    animator.SetBool("IsAttacking", false);
                }
                break;

            case EnemyState.Frustrated:
                // Frustrerat beteende - spring omkring slumpmässigt, attackera allt i närheten
                BehaveFrustrated();
                if (animator != null)
                {
                    animator.SetBool("IsChasing", true); // Använd jaga-animation för frustrerat tillstånd
                    animator.SetBool("IsAttacking", false);
                }
                break;
        }
    }

    private void BehaveFrustrated()
    {
        // Set högre hastighet för frustrerat tillstånd
        agent.speed = moveSpeed * 1.2f;

        // Om nära målet eller inget mål är satt, välj ny slumpmässig punkt
        if (agent.remainingDistance < 0.5f || !agent.hasPath)
        {
            // Välj en slumpmässig riktning med större radie än patrullradien
            Vector3 randomDirection = Random.insideUnitSphere * (patrolRadius * 1.5f);
            randomDirection.y = 0;

            // Sätt nästa punkt från nuvarande position (inte startposition)
            Vector3 nextPoint = transform.position + randomDirection;
            NavMeshHit hit;

            // Säkerställ att punkten finns på NavMesh
            if (NavMesh.SamplePosition(nextPoint, out hit, patrolRadius * 1.5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        // Attackera allt i närheten, inklusive förstörbara objekt
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (Collider collider in colliders)
        {
            // Attackera spelare om nära
            if (collider.CompareTag("Player"))
            {
                AttackPlayer();
                break;
            }

            // Attackera/förstör andra objekt om det finns någon mekanism för detta
            // Du kan implementera förstörbara objekt här senare
            // Exempel:
            // DestructibleObject destructible = collider.GetComponent<DestructibleObject>();
            // if (destructible != null)
            // {
            //     destructible.TakeDamage(attackDamage);
            // }
        }
    }

    private IEnumerator PatrolRoutine()
    {
        while (!isDead)
        {
            // Endast patrullera om i patrulltillstånd och inte väntar
            if (currentState == EnemyState.Patrol && !isWaitingAtPatrolPoint)
            {
                // Om nära målet eller inget mål är satt, välj ny patrullpunkt
                if (agent.remainingDistance < 0.5f || !agent.hasPath)
                {
                    // Vänta vid patrullpunkten
                    isWaitingAtPatrolPoint = true;
                    agent.isStopped = true;
                    float waitTime = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
                    yield return new WaitForSeconds(waitTime);

                    // Välj ny patrullpunkt
                    Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                    randomDirection.y = 0;

                    // Sätt nästa patrullpunkt inom räckhåll från startposition
                    Vector3 nextPatrolPoint = startPosition + randomDirection;
                    NavMeshHit hit;

                    // Säkerställ att punkten finns på NavMesh
                    if (NavMesh.SamplePosition(nextPatrolPoint, out hit, patrolRadius, NavMesh.AllAreas))
                    {
                        currentPatrolTarget = hit.position;
                        agent.SetDestination(currentPatrolTarget);
                        agent.isStopped = false;
                        isWaitingAtPatrolPoint = false;
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            // Vänd mot spelaren
            Vector3 directionToPlayer = player.position - transform.position;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToPlayer);
            }

            // Ge skada till spelaren
            PlayerStatus playerStatus = player.GetComponent<PlayerStatus>();
            if (playerStatus != null)
            {
                playerStatus.TakeDamage(attackDamage, PlayerStatus.DamageType.Combat);

                // Spela attackljud
                if (attackSound != null)
                {
                    audioSource.PlayOneShot(attackSound);
                }

                // Trigga attack-animation om animation finns
                if (animator != null)
                {
                    animator.SetTrigger("Attack");
                }
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

        // Öka frustration när man tar skada
        currentFrustration += damage * 0.5f;

        // Om hälsan når 0, dö
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Sätt tillstånd till Chase om spelaren attackerar
            currentState = EnemyState.Chase;
            isWaitingAtPatrolPoint = false;

            // Spela skada-animation om sådan finns
            if (animator != null)
            {
                animator.SetTrigger("TakeDamage");
            }
        }
    }

    private void Die()
    {
        isDead = true;

        // Spela dödsljud
        if (deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

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
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // Förstör objekt efter en viss tid (alternativt kan du välja att behålla liket)
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

        // Minska frustration när man äter
        currentFrustration -= foodValue * 0.5f;
        if (currentFrustration < 0)
        {
            currentFrustration = 0;
        }

        // Spela ät-animation om sådan finns
        if (animator != null)
        {
            animator.SetTrigger("Eat");
        }
    }

    private bool CheckIfOnSameFloor()
    {
        // Denna metod behöver implementeras för att avgöra om fienden och spelaren är på samma våning
        // Detta kan göras på olika sätt beroende på spel, t.ex. genom att jämföra Y-position
        // eller använda speciella triggers för varje våning

        if (player == null)
            return false;

        // Enkel implementation: Kontrollera om höjdskillnaden är mindre än ett tröskelvärde
        float heightDifference = Mathf.Abs(transform.position.y - player.position.y);
        return heightDifference < 2f; // Anta att varje våning är minst 2 enheter hög
    }

    private bool IsThereAnyFoodNearby()
    {
        // Sök efter objekt med FoodItem-skript inom en viss radie
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
        foreach (Collider collider in colliders)
        {
            if (collider.GetComponent<FoodItem>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void GoToNearestFood()
    {
        // Hitta närmaste tillgängliga mat
        FoodItem nearestFood = FindNearestFood();

        if (nearestFood != null)
        {
            // Gå till maten
            agent.SetDestination(nearestFood.transform.position);

            // Om nära nog att äta maten
            if (Vector3.Distance(transform.position, nearestFood.transform.position) < 1.5f)
            {
                // Simulera att äta maten genom att anropa Eat-metoden på både fienden och matföremålet
                Eat(20f); // Hungervärde att återställa

                // Förstör matföremålet om det är konfigurerat att försvinna efter att ha ätits
                // Detta kommer matföremålet att hantera själv när man kolliderar med det
            }
        }
        else
        {
            // Om ingen mat hittades, återgå till patrullering
            currentState = EnemyState.Patrol;
        }
    }

    private FoodItem FindNearestFood()
    {
        // Sök efter objekt med FoodItem-skript inom detekteringsradien
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);

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

    private void SearchForFood()
    {
        // Sök mat mer aktivt, större radie än normalt
        FoodItem nearestFood = FindNearestFood();

        if (nearestFood != null)
        {
            // Gå till maten med högre hastighet
            agent.speed = moveSpeed;
            agent.SetDestination(nearestFood.transform.position);
        }
        else
        {
            // Om ingen mat hittas, sök i en annan del av nivån
            Vector3 randomSearchPoint = startPosition + Random.insideUnitSphere * (patrolRadius * 1.5f);
            randomSearchPoint.y = transform.position.y; // Behåll samma höjd

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomSearchPoint, out hit, patrolRadius * 1.5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
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
        // Rita detekteringsradie
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rita attackradie
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Rita patrullradie
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(startPosition, patrolRadius);
    }

    // Publika egenskaper och metoder för att få status och värden
    public bool IsDead => isDead;

    // Hämtar nuvarande hälsoprocent (0-1)
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    // Hämtar nuvarande hungerprocent (0-1)
    public float GetHungerPercentage()
    {
        return currentHunger / maxHunger;
    }

    // Hämtar nuvarande frustrationsprocent (0-1)
    public float GetFrustrationPercentage()
    {
        return currentFrustration / maxFrustration;
    }

    // Hämtar max hälsa
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    // Hämtar nuvarande hälsa
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // Hämtar max hunger
    public float GetMaxHunger()
    {
        return maxHunger;
    }

    // Hämtar nuvarande hunger
    public float GetCurrentHunger()
    {
        return currentHunger;
    }

    // Hämtar max frustration
    public float GetMaxFrustration()
    {
        return maxFrustration;
    }

    // Hämtar nuvarande frustration
    public float GetCurrentFrustration()
    {
        return currentFrustration;
    }
}