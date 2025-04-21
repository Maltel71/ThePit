using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kontrollerar ett vapens beteende, inklusive attackf�rm�ga, skada och cooldown.
/// Vapnet kan vara antingen ett f�rem�l som spelaren plockar upp eller en del av spelaren.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Vapenstatistik")]
    [SerializeField] private float damage = 15f; // Skada som vapnet g�r vid tr�ff
    [SerializeField] private float attackCooldown = 0.7f; // Tid mellan attacker
    [SerializeField] private float attackRange = 2f; // R�ckvidd f�r attacken
    [SerializeField] private float attackAngle = 60f; // Tr�ffvinkel framf�r spelaren (i grader)

    [Header("Effekter")]
    [SerializeField] private ParticleSystem hitEffect; // Partikeleffekt vid tr�ff
    [SerializeField] private GameObject attackVisual; // Visuell representation av attacksvepet
    [SerializeField] private float visualDuration = 0.2f; // Hur l�nge attackvisualiseringen visas

    [Header("Ljud")]
    [SerializeField] private AudioClip swingSound; // Ljud n�r man svingar vapnet
    [SerializeField] private AudioClip hitSound; // Ljud vid tr�ff
    [SerializeField] private AudioClip equipSound; // Ljud n�r man tar upp vapnet

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

        // Hitta spelaren om vapnet �r ett frist�ende objekt
        player = transform.root;
        if (player.CompareTag("Player"))
        {
            playerInput = player.GetComponent<StarterAssets.StarterAssetsInputs>();
        }

        // St�ng av attackvisualisering vid start
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
    /// Utf�r ett attacksvep i omr�det framf�r spelaren.
    /// Skadar fiender och f�ngar inom r�ckvidden och vinkeln.
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

        // Hitta potentiella m�l inom attackr�ckvidden
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        bool hitSomething = false;

        foreach (Collider hit in hitColliders)
        {
            // Ignorera spelaren sj�lv
            if (hit.transform == player || hit.transform.IsChildOf(player))
                continue;

            // Ber�kna riktning till m�let
            Vector3 direction = hit.transform.position - transform.position;
            direction.y = 0; // Ignorera h�jdskillnad

            // Kontrollera om m�let �r inom attackvinkeln
            float angle = Vector3.Angle(player.forward, direction);
            if (angle <= attackAngle * 0.5f)
            {
                // F�rs�k hitta fiende eller f�nge att skada
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

                // Visa tr�ffeffekt om n�got tr�ffades
                if (hitSomething && hitEffect != null)
                {
                    Vector3 hitPosition = hit.ClosestPoint(transform.position);
                    hitEffect.transform.position = hitPosition;
                    hitEffect.Play();

                    // Spela tr�ffljud
                    if (hitSound != null)
                    {
                        audioSource.PlayOneShot(hitSound);
                    }
                }
            }
        }

        // �terst�ll attack-cooldown
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

    // Metod f�r att aktivera vapnet n�r spelaren plockar upp det
    public void Equip()
    {
        if (equipSound != null)
        {
            audioSource.PlayOneShot(equipSound);
        }
    }

    // F�r visuell debugging i Unity Editor
    private void OnDrawGizmosSelected()
    {
        // Rita attackr�ckvidd
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Rita attackvinkel
        Gizmos.color = Color.yellow;
        Vector3 rightDir = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward;

        Gizmos.DrawRay(transform.position, rightDir * attackRange);
        Gizmos.DrawRay(transform.position, leftDir * attackRange);

        // Rita en b�gad linje f�r att representera attackvinkeln
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