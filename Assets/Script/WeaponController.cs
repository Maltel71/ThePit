using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kontrollerar ett vapens beteende, inklusive attackförmåga, skada och cooldown.
/// Vapnet kan vara antingen ett föremål som spelaren plockar upp eller en del av spelaren.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Vapenstatistik")]
    [SerializeField] private float damage = 15f; // Skada som vapnet gör vid träff
    [SerializeField] private float attackCooldown = 0.7f; // Tid mellan attacker
    [SerializeField] private float attackRange = 2f; // Räckvidd för attacken
    [SerializeField] private float attackAngle = 60f; // Träffvinkel framför spelaren (i grader)

    [Header("Effekter")]
    [SerializeField] private ParticleSystem hitEffect; // Partikeleffekt vid träff
    [SerializeField] private GameObject attackVisual; // Visuell representation av attacksvepet
    [SerializeField] private float visualDuration = 0.2f; // Hur länge attackvisualiseringen visas

    [Header("Ljud")]
    [SerializeField] private AudioClip swingSound; // Ljud när man svingar vapnet
    [SerializeField] private AudioClip hitSound; // Ljud vid träff
    [SerializeField] private AudioClip equipSound; // Ljud när man tar upp vapnet

    // Privata variabler
    private bool canAttack = true;
    private float lastAttackTime = -999f;
    private AudioSource audioSource;
    private Transform player;
    private StarterAssets.StarterAssetsInputs playerInput;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Hitta spelaren om vapnet är ett fristående objekt
        player = transform.root;
        if (player.CompareTag("Player"))
        {
            playerInput = player.GetComponent<StarterAssets.StarterAssetsInputs>();
        }

        // Stäng av attackvisualisering vid start
        if (attackVisual != null)
        {
            attackVisual.SetActive(false);
        }
    }

    private void Update()
    {
        // Kontrollera om spelaren vill attackera
        //if (playerInput != null && playerInput.attack && canAttack)
        {
            Attack();
        }
    }

    /// <summary>
    /// Utför ett attacksvep i området framför spelaren.
    /// Skadar fiender och fångar inom räckvidden och vinkeln.
    /// </summary>
    public void Attack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        canAttack = false;

        // Spela svingljud
        if (swingSound != null)
        {
            audioSource.PlayOneShot(swingSound);
        }

        // Visa attackvisualisering
        if (attackVisual != null)
        {
            StartCoroutine(ShowAttackVisual());
        }

        // Hitta potentiella mål inom attackräckvidden
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        bool hitSomething = false;

        foreach (Collider hit in hitColliders)
        {
            // Ignorera spelaren själv
            if (hit.transform == player || hit.transform.IsChildOf(player))
                continue;

            // Beräkna riktning till målet
            Vector3 direction = hit.transform.position - transform.position;
            direction.y = 0; // Ignorera höjdskillnad

            // Kontrollera om målet är inom attackvinkeln
            float angle = Vector3.Angle(player.forward, direction);
            if (angle <= attackAngle * 0.5f)
            {
                // Försök hitta fiende eller fånge att skada
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    hitSomething = true;
                }
                else
                {
                    PrisonerController prisoner = hit.GetComponent<PrisonerController>();
                    if (prisoner != null)
                    {
                        prisoner.TakeDamage(damage);
                        hitSomething = true;
                    }
                }

                // Visa träffeffekt om något träffades
                if (hitSomething && hitEffect != null)
                {
                    Vector3 hitPosition = hit.ClosestPoint(transform.position);
                    hitEffect.transform.position = hitPosition;
                    hitEffect.Play();

                    // Spela träffljud
                    if (hitSound != null)
                    {
                        audioSource.PlayOneShot(hitSound);
                    }
                }
            }
        }

        // Återställ attack-cooldown
        StartCoroutine(ResetAttackCooldown());
    }

    private IEnumerator ShowAttackVisual()
    {
        attackVisual.SetActive(true);
        yield return new WaitForSeconds(visualDuration);
        attackVisual.SetActive(false);
    }

    private IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // Metod för att aktivera vapnet när spelaren plockar upp det
    public void Equip()
    {
        if (equipSound != null)
        {
            audioSource.PlayOneShot(equipSound);
        }
    }

    // För visuell debugging i Unity Editor
    private void OnDrawGizmosSelected()
    {
        // Rita attackräckvidd
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Rita attackvinkel
        Gizmos.color = Color.yellow;
        Vector3 rightDir = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward;

        Gizmos.DrawRay(transform.position, rightDir * attackRange);
        Gizmos.DrawRay(transform.position, leftDir * attackRange);

        // Rita en bågad linje för att representera attackvinkeln
        int segments = 20;
        float angleStep = attackAngle / segments;
        Vector3 prevPos = transform.position + leftDir * attackRange;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -attackAngle * 0.5f + angleStep * i;
            Vector3 currentDir = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
            Vector3 currentPos = transform.position + currentDir * attackRange;

            Gizmos.DrawLine(prevPos, currentPos);
            prevPos = currentPos;
        }
    }
}