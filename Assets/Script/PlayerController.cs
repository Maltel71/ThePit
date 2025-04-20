using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("R�relse")]
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
            Debug.LogError("CharacterController saknas p� spelaren");
        }

        cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        // G�m muspekaren och l�s den till spelets centrum
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Kolla om spelaren st�r p� marken
        isGrounded = IsGrounded();

        // Hantera r�relse och hopp
        HandleMovement();
        HandleGravity();

        // Applicera r�relse
        controller.Move(velocity * Time.deltaTime);

        // V�xla mellan l�st och ol�st muspekare med Escape-tangenten
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    private void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // L�s upp muspekaren
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // L�s muspekaren
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleMovement()
    {
        // L�s input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Best�m r�relseriktning relativt till kameran
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Ber�kna r�relseriktning baserat p� kamerans vy
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;

            // Rotera spelaren gradvis mot riktningen
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(0f, targetAngle, 0f),
                rotationSpeed * Time.deltaTime
            );

            // Skapa r�relsevektorn fram�t relativt spelarens rotation
            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // Applicera hastighet
            velocity.x = moveDirection.x * moveSpeed;
            velocity.z = moveDirection.z * moveSpeed;
        }
        else
        {
            // Om ingen input, stanna horisontell r�relse gradvis (f�r mjukare k�nsla)
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
        // Applicera gravitation om inte p� marken
        if (isGrounded && yVelocity < 0)
        {
            yVelocity = -0.5f; // En liten negativ kraft f�r att h�lla spelaren p� marken
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
        // Kolla om spelaren st�r p� marken med en liten sphere cast ned�t
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

    // F�r debugging
    private void OnDrawGizmos()
    {
        // Rita markkontroll
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.4f, 0.3f);
    }
}