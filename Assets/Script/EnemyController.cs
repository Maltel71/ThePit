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
    [SerializeField] private float detectionRange = 10f; // Hur l�ngt fienden kan uppt�cka spelaren
    [SerializeField] private float attackRange = 2f; // Avst�nd f�r att attackera
    [SerializeField] private float attackCooldown = 1.5f; // Tid mellan attacker
    [SerializeField] private float attackDamage = 10f; // Skada per attack
    [SerializeField] private float moveSpeed = 3.5f; // R�relsehastighet vid jakt
    [SerializeField] private float patrolSpeed = 1.5f; // R�relsehastighet vid patrullering
    [SerializeField] private float minPatrolWaitTime = 1f; // Min v�ntetid mellan patrullpunkter
    [SerializeField] private float maxPatrolWaitTime = 4f; // Max v�ntetid mellan patrullpunkter
    [SerializeField] private float patrolRadius = 10f; // Maximal radie f�r patrullpunkter

    [Header("Floor Detection")]
    [SerializeField] private Transform floorCheck; // Punkt f�r att detektera vilken v�ning fienden �r p�
    [SerializeField] private LayerMask floorLayer; // Layer f�r v�ningar

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

    // Tillst�nd f�r AI
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

        // S�tt startposition f�r patrullering
        startPosition = transform.position;
        currentPatrolTarget = startPosition;

        // Hitta spelaren
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Start()
    {
        // Initiera h�lsa och hunger
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
            Debug.LogError("NavMeshAgent saknas p� fienden!");
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

        // Kontrollera om spelaren �r inom detekteringsavst�nd
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isPlayerInRange = distanceToPlayer <= detectionRange;

        // Uppdatera AI-tillst�nd baserat p� situation
        UpdateAIState(distanceToPlayer);

        // Utf�r handlingar baserat p� aktuellt tillst�nd
        PerformStateAction();
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
            TakeDamage(0.5f * Time.deltaTime);
        }
    }

    private void UpdateAIState(float distanceToPlayer)
    {
        // Kontrollera om fienden �r p� samma v�ning som spelaren
        bool isOnSameFloor = CheckIfOnSameFloor();

        // Prioritera att �ta om hungern �r l�g och mat finns tillg�nglig
        if (currentHunger < 30f && IsThereAnyFoodNearby())
        {
            currentState = EnemyState.Eat;
            return;
        }

        // Prioritera att jaga om spelaren �r inom r�ckh�ll och p� samma v�ning
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
            // Om fienden �r hungrig men inte super-hungrig, leta efter mat
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
                isWaitingAtPatrolPoint = false; // Avbryt patrullv�ntan om den jagar
                agent.speed = moveSpeed;
                agent.SetDestination(player.position);
                if (animator != null)
                {
                    animator.SetBool("IsChasing", true);
                    animator.SetBool("IsAttacking", false);
                }
                break;

            case EnemyState.Attack:
                agent.SetDestination(transform.position); // Stoppa r�relse vid attack
                AttackPlayer();
                if (animator != null)
                {
                    animator.SetBool("IsChasing", false);
                    animator.SetBool("IsAttacking", true);
                }
                break;

            case EnemyState.Eat:
                // Hitta n�rmaste mat och g� till den
                GoToNearestFood();
                if (animator != null)
                {
                    animator.SetBool("IsChasing", false);
                    animator.SetBool("IsAttacking", false);
                }
                break;

            case EnemyState.Hungry:
                // S�k efter mat mer aktivt n�r hungrig
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
            // Endast patrullera om i patrulltillst�nd och inte v�ntar
            if (currentState == EnemyState.Patrol && !isWaitingAtPatrolPoint)
            {
                // Om n�ra m�let eller inget m�l �r satt, v�lj ny patrullpunkt
                if (agent.remainingDistance < 0.5f || !agent.hasPath)
                {
                    // V�nta vid patrullpunkten
                    isWaitingAtPatrolPoint = true;
                    agent.isStopped = true;
                    float waitTime = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
                    yield return new WaitForSeconds(waitTime);

                    // V�lj ny patrullpunkt
                    Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                    randomDirection.y = 0;

                    // S�tt n�sta patrullpunkt inom r�ckh�ll fr�n startposition
                    Vector3 nextPatrolPoint = startPosition + randomDirection;
                    NavMeshHit hit;

                    // S�kerst�ll att punkten finns p� NavMesh
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

            // V�nd mot spelaren
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

        // Om h�lsan n�r 0, d�
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // S�tt tillst�nd till Chase om spelaren attackerar
            currentState = EnemyState.Chase;
            isWaitingAtPatrolPoint = false;

            // Spela skada-animation om s�dan finns
            if (animator != null)
            {
                animator.SetTrigger("TakeDamage");
            }
        }
    }

    private void Die()
    {
        isDead = true;

        // Spela d�dsljud
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

        // Spela d�d-animation om s�dan finns
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

        // F�rst�r objekt efter en viss tid (alternativt kan du v�lja att beh�lla liket)
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

        // Spela �t-animation om s�dan finns
        if (animator != null)
        {
            animator.SetTrigger("Eat");
        }
    }

    private bool CheckIfOnSameFloor()
    {
        // Denna metod beh�ver implementeras f�r att avg�ra om fienden och spelaren �r p� samma v�ning
        // Detta kan g�ras p� olika s�tt beroende p� spel, t.ex. genom att j�mf�ra Y-position
        // eller anv�nda speciella triggers f�r varje v�ning

        if (player == null)
            return false;

        // Enkel implementation: Kontrollera om h�jdskillnaden �r mindre �n ett tr�skelv�rde
        float heightDifference = Mathf.Abs(transform.position.y - player.position.y);
        return heightDifference < 2f; // Anta att varje v�ning �r minst 2 enheter h�g
    }

    private bool IsThereAnyFoodNearby()
    {
        // S�k efter objekt med FoodItem-skript inom en viss radie
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
        // Hitta n�rmaste tillg�ngliga mat
        FoodItem nearestFood = FindNearestFood();

        if (nearestFood != null)
        {
            // G� till maten
            agent.SetDestination(nearestFood.transform.position);

            // Om n�ra nog att �ta maten
            if (Vector3.Distance(transform.position, nearestFood.transform.position) < 1.5f)
            {
                // Simulera att �ta maten genom att anropa Eat-metoden p� b�de fienden och matf�rem�let
                Eat(20f); // Hungerv�rde att �terst�lla

                // F�rst�r matf�rem�let om det �r konfigurerat att f�rsvinna efter att ha �tits
                // Detta kommer matf�rem�let att hantera sj�lv n�r man kolliderar med det
            }
        }
        else
        {
            // Om ingen mat hittades, �terg� till patrullering
            currentState = EnemyState.Patrol;
        }
    }

    private FoodItem FindNearestFood()
    {
        // S�k efter objekt med FoodItem-skript inom detekteringsradien
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
        // S�k mat mer aktivt, st�rre radie �n normalt
        FoodItem nearestFood = FindNearestFood();

        if (nearestFood != null)
        {
            // G� till maten med h�gre hastighet
            agent.speed = moveSpeed;
            agent.SetDestination(nearestFood.transform.position);
        }
        else
        {
            // Om ingen mat hittas, s�k i en annan del av niv�n
            Vector3 randomSearchPoint = startPosition + Random.insideUnitSphere * (patrolRadius * 1.5f);
            randomSearchPoint.y = transform.position.y; // Beh�ll samma h�jd

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
            // Eftersom FoodItem redan har kollisionslogik, kommer den att hantera �t-logiken
            // Men vi kan ocks� manuellt �terst�lla hunger h�r om det beh�vs
        }
    }

    // F�r visuell debugging
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