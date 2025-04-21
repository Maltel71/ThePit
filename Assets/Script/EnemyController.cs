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

    [Header("Floor Detection")]
    [SerializeField] private Transform floorCheck; // Punkt för att detektera vilken våning fienden är på
    [SerializeField] private LayerMask floorLayer; // Layer för våningar

    [Header("Ljud")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

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

    // Tillstånd för AI
    private enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Eat,
        Hungry
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

        // Om hunger är väldigt låg, ta skada
        if (currentHunger <= 5f)
        {
            TakeDamage(0.5f * Time.deltaTime);
        }
    }

    private void UpdateAIState(float distanceToPlayer)
    {
        // Kontrollera om fienden är på samma våning som spelaren
        bool isOnSameFloor = CheckIfOnSameFloor();

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
}