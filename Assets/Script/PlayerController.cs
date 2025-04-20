using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Rörelse")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Debug")]
    [SerializeField] private bool isGrounded;

    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private Transform cameraTransform;
    private float yVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("CharacterController saknas på spelaren");
        }

        cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        // Göm muspekaren och lås den till spelets centrum
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Kolla om spelaren står på marken
        isGrounded = IsGrounded();

        // Hantera rörelse och hopp
        HandleMovement();
        HandleGravity();

        // Applicera rörelse
        controller.Move(velocity * Time.deltaTime);

        // Växla mellan låst och olåst muspekare med Escape-tangenten
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    private void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Lås upp muspekaren
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Lås muspekaren
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleMovement()
    {
        // Läs input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Bestäm rörelseriktning relativt till kameran
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Beräkna rörelseriktning baserat på kamerans vy
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;

            // Rotera spelaren gradvis mot riktningen
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(0f, targetAngle, 0f),
                rotationSpeed * Time.deltaTime
            );

            // Skapa rörelsevektorn framåt relativt spelarens rotation
            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // Applicera hastighet
            velocity.x = moveDirection.x * moveSpeed;
            velocity.z = moveDirection.z * moveSpeed;
        }
        else
        {
            // Om ingen input, stanna horisontell rörelse gradvis (för mjukare känsla)
            velocity.x = Mathf.Lerp(velocity.x, 0f, 10f * Time.deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, 0f, 10f * Time.deltaTime);
        }

        // Hantera hopp
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            yVelocity = jumpForce;
        }
    }

    private void HandleGravity()
    {
        // Applicera gravitation om inte på marken
        if (isGrounded && yVelocity < 0)
        {
            yVelocity = -0.5f; // En liten negativ kraft för att hålla spelaren på marken
        }
        else
        {
            yVelocity -= gravity * Time.deltaTime;
        }

        // Uppdatera y-hastigheten
        velocity.y = yVelocity;
    }

    private bool IsGrounded()
    {
        // Kolla om spelaren står på marken med en liten sphere cast nedåt
        float sphereRadius = 0.3f;
        float distance = 0.4f;

        return Physics.SphereCast(
            transform.position,
            sphereRadius,
            Vector3.down,
            out RaycastHit hit,
            distance,
            groundLayer
        );
    }

    // För debugging
    private void OnDrawGizmos()
    {
        // Rita markkontroll
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.4f, 0.3f);
    }
}