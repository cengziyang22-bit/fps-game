using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 12f;
    public float crouchSpeed = 3f;
    public float adsSpeedMultiplier = 0.5f;
    public float acceleration = 15f;
    public float gravity = 22f;
    public float jumpForce = 6f;
    public float crouchHeight = 0.9f;
    public float standHeight = 1.8f;
    public float crouchTransitionSpeed = 14f;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("ADS")]
    public Camera playerCamera;
    public float baseFov = 70f;
    public float adsTransitionSpeed = 12f;

    // Public state
    [HideInInspector] public bool isADS = false;
    [HideInInspector] public bool isCrouching = false;
    [HideInInspector] public bool isSprinting = false;
    [HideInInspector] public bool isGrounded = true;

    private CharacterController cc;
    private Vector3 velocity;
    private float xRotation = 0f;
    private float currentSpeed;
    private float targetHeight;
    private Vector3 smoothVelocity;
    private float currentFov;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        targetHeight = standHeight;
        cc.height = standHeight;
        currentFov = baseFov;
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameOver) return;

        HandleLook();
        HandleADS();
        HandleCrouch();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleADS()
    {
        isADS = Input.GetMouseButton(1);
        var wm = GetComponent<WeaponManager>();
        float targetFov = (isADS && wm != null) ? wm.CurrentWeapon.adsFov : baseFov;
        currentFov = Mathf.Lerp(currentFov, targetFov, adsTransitionSpeed * Time.deltaTime);
        playerCamera.fieldOfView = currentFov;
    }

    void HandleCrouch()
    {
        isCrouching = Input.GetKey(KeyCode.C);
        targetHeight = isCrouching ? crouchHeight : standHeight;
        cc.height = Mathf.Lerp(cc.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Adjust camera height
        float eyeOffset = isCrouching ? 0.45f : 0.9f;
        Vector3 camPos = playerCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, eyeOffset, crouchTransitionSpeed * Time.deltaTime);
        playerCamera.transform.localPosition = camPos;
    }

    void HandleMovement()
    {
        isSprinting = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        float target = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
        if (isADS) target *= adsSpeedMultiplier;
        currentSpeed = Mathf.Lerp(currentSpeed, target, 10f * Time.deltaTime);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        if (move.magnitude > 1f) move.Normalize();

        smoothVelocity = Vector3.Lerp(smoothVelocity, move * currentSpeed, acceleration * Time.deltaTime);

        Vector3 motion = smoothVelocity * Time.deltaTime;
        motion.y = velocity.y * Time.deltaTime;
        cc.Move(motion);

        isGrounded = cc.isGrounded;
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = jumpForce;
        }
    }

    void ApplyGravity()
    {
        if (cc.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        else
            velocity.y -= gravity * Time.deltaTime;
    }

    public void ResetState()
    {
        velocity = Vector3.zero;
        xRotation = 0f;
        isADS = false;
        isCrouching = false;
        isSprinting = false;
        currentFov = baseFov;
        playerCamera.fieldOfView = baseFov;
        cc.height = standHeight;
    }
}
