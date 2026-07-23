using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Acceleration while grounded.")]
    [SerializeField] private float groundAcceleration = 120f;

    [Tooltip("Acceleration while airborne.")]
    [SerializeField] private float airAcceleration = 80f;

    [Tooltip("Deceleration while grounded.")]
    [SerializeField] private float groundDeceleration = 140f;

    [Tooltip("Deceleration while airborne.")]
    [SerializeField] private float airDeceleration = 60f;

    [Tooltip("Maximum horizontal speed.")]
    [SerializeField] private float maxSpeed = 10f;

    [Header("Jump")]
    [Tooltip("Impulse applied on a jump.")]
    [SerializeField] private float jumpForce = 16.5f;

    [Tooltip("Multiplier applied to upward velocity when jump is released early.")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Tooltip("How long the player can still jump after leaving the ground.")]
    [SerializeField] private float coyoteTime = 0.1f;

    [Tooltip("How long jump input is remembered before landing.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Tooltip("How long dash input is remembered before landing or becoming available.")]
    [SerializeField] private float dashBufferTime = 0.1f;

    [Header("Corner Correction (Box Precision)")]
    [Tooltip("Enable ceiling corner correction to smooth out head-clips on box corners.")]
    [SerializeField] private bool enableCornerCorrection = true;

    [Tooltip("How far upward to check for corner overhangs.")]
    [SerializeField] private float cornerCheckDistance = 0.25f;

    [Tooltip("How many units to nudge the player horizontally when clipping a ceiling corner.")]
    [SerializeField] private float cornerNudgeAmount = 0.12f;

    [Header("Gravity")]
    [Tooltip("Gravity while rising.")]
    [SerializeField] private float riseGravity = 40f;

    [Tooltip("Gravity while falling.")]
    [SerializeField] private float fallGravity = 65f;

    [Tooltip("Lower gravity near the jump apex to make motion feel smoother.")]
    [SerializeField] private float apexGravity = 28f;

    [Tooltip("Maximum downward speed.")]
    [SerializeField] private float maxFallSpeed = 28f;

    [Header("Wall & Wall Jump")]
    [Tooltip("Wall slide speed when sliding down a wall without climbing.")]
    [SerializeField] private float wallSlideSpeed = 4f;

    [Tooltip("Horizontal force applied when wall jumping.")]
    [SerializeField] private float wallJumpHorizontalForce = 12f;

    [Tooltip("Vertical force applied when wall jumping.")]
    [SerializeField] private float wallJumpVerticalForce = 16.5f;

    [Tooltip("How long the player is locked out from wall jumping control.")]
    [SerializeField] private float wallJumpLockTime = 0.15f;

    [Header("Wall Climbing & Stamina")]
    [Tooltip("Maximum stamina pool available for climbing.")]
    [SerializeField] private float maxStamina = 100f;

    [Tooltip("Speed when climbing upward on a wall.")]
    [SerializeField] private float climbUpSpeed = 4.5f;

    [Tooltip("Speed when climbing downward on a wall.")]
    [SerializeField] private float climbDownSpeed = 6.5f;

    [Tooltip("Stamina drained per second while clinging still on a wall.")]
    [SerializeField] private float climbStaminaDrain = 18f;

    [Tooltip("Stamina drained per second while actively climbing upward.")]
    [SerializeField] private float climbUpStaminaDrain = 55f;

    [Tooltip("Stamina drained per second while climbing downward.")]
    [SerializeField] private float climbDownStaminaDrain = 8f;

    [Tooltip("Stamina deducted when performing a wall jump.")]
    [SerializeField] private float wallJumpStaminaCost = 32f;

    [Tooltip("When exhausted (0 stamina), allow a weak horizontal push wall jump with no upward force?")]
    [SerializeField] private bool allowExhaustedWallJump = true;

    [Header("Low Stamina Warning VFX")]
    [Tooltip("Percentage threshold (0-1) where low stamina blinking starts.")]
    [SerializeField][Range(0.1f, 0.5f)] private float lowStaminaThresholdRatio = 0.3f;

    [Tooltip("Color to tint/flash the player sprite when low on stamina.")]
    [SerializeField] private Color lowStaminaFlashColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Tooltip("How fast the player sprite blinks when stamina is low.")]
    [SerializeField] private float lowStaminaBlinkSpeed = 16f;

    [Header("Ledge Climb / Hop")]
    [Tooltip("Enable automated climbing onto ledges when climbing past the wall top.")]
    [SerializeField] private bool enableLedgeClimb = true;

    [Tooltip("Height offset above the player to check for free space over the ledge.")]
    [SerializeField] private float ledgeCheckHeight = 0.6f;

    [Tooltip("Distance forward to check over the top of the ledge.")]
    [SerializeField] private float ledgeCheckForwardDistance = 0.5f;

    [Tooltip("Upward force boost when popping onto a ledge.")]
    [SerializeField] private float ledgeClimbUpForce = 8f;

    [Tooltip("Forward force boost toward the ledge surface.")]
    [SerializeField] private float ledgeClimbForwardForce = 5f;

    [Tooltip("Duration of movement control lock during ledge hop.")]
    [SerializeField] private float ledgeClimbLockDuration = 0.12f;

    [Header("Dash (8-Way)")]
    [Tooltip("Dash speed.")]
    [SerializeField] private float dashSpeed = 24f;

    [Tooltip("How long the dash lasts.")]
    [SerializeField] private float dashDuration = 0.15f;

    [Tooltip("Optional cooldown between dashes.")]
    [SerializeField] private float dashCooldown = 0.2f;

    [Tooltip("Multiplier applied to Y speed when an upward dash ends.")]
    [SerializeField][Range(0.1f, 1f)] private float upwardDashEndMultiplier = 0.5f;

    [Tooltip("Optional brief freeze frame when dashing.")]
    [SerializeField] private bool enableDashFreezeFrame = false;

    [Tooltip("Time scale used during the dash freeze frame.")]
    [SerializeField][Range(0.05f, 1f)] private float dashFreezeFrameTimeScale = 0.2f;

    [Tooltip("Duration of the dash freeze frame.")]
    [SerializeField] private float dashFreezeFrameDuration = 0.04f;

    [Header("Collision Checks")]
    [Tooltip("Radius of the ground check overlap circle.")]
    [SerializeField] private float groundCheckRadius = 0.18f;

    [Tooltip("Radius of the wall check overlap circle.")]
    [SerializeField] private float wallCheckRadius = 0.16f;

    [Header("Required References")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask collisionMask;

    [Header("Death and Respawn")]
    [Tooltip("The Layer representing hazards/damage.")]
    [SerializeField] private LayerMask damageLayer;
    [Tooltip("Fixed transform point where the player will respawn.")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("How many seconds to wait in the death state before reviving.")]
    [SerializeField] private float respawnDelay = 1.2f;

    [Header("Animation State Names")]
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private string idleNoDashStateName = "idle_no_dash";
    [SerializeField] private string runStateName = "run";
    [SerializeField] private string runNoDashStateName = "run_no_dash";
    [SerializeField] private string jumpStateName = "jump";
    [SerializeField] private string jumpNoDashStateName = "jump_no_dash";
    [SerializeField] private string fallStateName = "fall";
    [SerializeField] private string fallNoDashStateName = "fall_no_dash";
    [SerializeField] private string climbStateName = "climb";
    [SerializeField] private string climbNoDashStateName = "climb_no_dash";
    [SerializeField] private string deathStateName = "death";
    [SerializeField] private string deathNoDashStateName = "death_no_dash";

    [Header("Animation References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteFacesRightByDefault = true;
    [SerializeField] private float runAnimSpeedThreshold = 0.1f;

    [Header("Dash VFX (Afterimages)")]
    [SerializeField] private Color dashAfterimageColor = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private float dashAfterimageInterval = 0.02f;
    [SerializeField] private float dashAfterimageFadeDuration = 0.25f;
    [SerializeField] private int dashAfterimageSortingOrderOffset = -1;
    [SerializeField] private bool enableDashFlash = true;
    [SerializeField] private Color dashFlashColor = Color.white;
    [SerializeField] private float dashFlashDuration = 0.06f;

    [Header("Spring Integration")]
    [SerializeField] private float horizontalSpringLockTime = 0.2f;
    [SerializeField] private float springJumpBoost = 6f;
    [SerializeField] private float springGraceTime = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // References
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    // Hardcoded Input System Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction grabAction;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool jumpReleased;
    private bool dashPressed;
    private bool climbHeld;

    private bool isGrounded;
    private bool isTouchingWall;
    private int wallDirection;
    private bool isWallSliding;
    private bool isClimbing;
    private bool isLedgeClimbing;
    private float currentStamina;

    private bool isDashing;
    private Vector2 dashDirection;
    private bool dashAvailable = true;
    private int facingDirection = 1;

    private float jumpBufferTimer;
    private float dashBufferTimer;
    private float coyoteTimer;
    private float wallJumpLockTimer;
    private float ledgeClimbTimer;
    private float dashCooldownTimer;
    private float dashTimer;
    private float springLockTimer;
    private float springGraceTimer;

    private Coroutine freezeFrameRoutine;
    private bool isDead;
    private float dashAfterimageTimer;
    private int currentAnimHash;
    private Coroutine dashFlashRoutine;
    private Vector2 startPosition;

    // Animation Hashes
    private int animIdle;
    private int animIdleNoDash;
    private int animRun;
    private int animRunNoDash;
    private int animJump;
    private int animJumpNoDash;
    private int animFall;
    private int animFallNoDash;
    private int animClimb;
    private int animClimbNoDash;
    private int animDeath;
    private int animDeathNoDash;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsExhausted => currentStamina <= 0f;
    public bool IsClimbing => isClimbing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        startPosition = transform.position;
        currentStamina = maxStamina;

        CacheAnimHashes();
        SetupHardcodedInputs();
    }

    private void SetupHardcodedInputs()
    {
        moveAction = new InputAction("Move", type: InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        jumpAction = new InputAction("Jump", type: InputActionType.Button);
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        jumpAction.AddBinding("<Gamepad>/buttonNorth");
        jumpAction.AddBinding("<Keyboard>/c");
        jumpAction.AddBinding("<Keyboard>/space");

        dashAction = new InputAction("Dash", type: InputActionType.Button);
        dashAction.AddBinding("<Gamepad>/buttonWest");
        dashAction.AddBinding("<Gamepad>/buttonEast");
        dashAction.AddBinding("<Keyboard>/x");
        dashAction.AddBinding("<Keyboard>/leftShift");

        grabAction = new InputAction("Grab", type: InputActionType.Button);
        grabAction.AddBinding("<Gamepad>/leftTrigger");
        grabAction.AddBinding("<Gamepad>/rightTrigger");
        grabAction.AddBinding("<Gamepad>/leftShoulder");
        grabAction.AddBinding("<Gamepad>/rightShoulder");
        grabAction.AddBinding("<Keyboard>/z");
        grabAction.AddBinding("<Keyboard>/leftCtrl");
    }

    private void CacheAnimHashes()
    {
        animIdle = Animator.StringToHash(idleStateName);
        animIdleNoDash = Animator.StringToHash(idleNoDashStateName);
        animRun = Animator.StringToHash(runStateName);
        animRunNoDash = Animator.StringToHash(runNoDashStateName);
        animJump = Animator.StringToHash(jumpStateName);
        animJumpNoDash = Animator.StringToHash(jumpNoDashStateName);
        animFall = Animator.StringToHash(fallStateName);
        animFallNoDash = Animator.StringToHash(fallNoDashStateName);
        animClimb = Animator.StringToHash(climbStateName);
        animClimbNoDash = Animator.StringToHash(climbNoDashStateName);
        animDeath = Animator.StringToHash(deathStateName);
        animDeathNoDash = Animator.StringToHash(deathNoDashStateName);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        dashAction?.Enable();
        grabAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        dashAction?.Disable();
        grabAction?.Disable();
    }

    private void Update()
    {
        if (isDead) return;

        moveInput = moveAction.ReadValue<Vector2>();
        jumpPressed = jumpAction.WasPressedThisFrame();
        jumpHeld = jumpAction.IsPressed();
        jumpReleased = jumpAction.WasReleasedThisFrame();
        dashPressed = dashAction.WasPressedThisFrame();
        climbHeld = grabAction.IsPressed();

        if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        if (dashPressed) dashBufferTimer = dashBufferTime;

        if (jumpReleased && !isGrounded && rb.linearVelocity.y > 0f && !isClimbing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        if (isClimbing || isWallSliding)
        {
            facingDirection = wallDirection;
        }
        else if (moveInput.x != 0f)
        {
            facingDirection = moveInput.x > 0f ? 1 : -1;
        }

        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);
        dashBufferTimer = Mathf.Max(0f, dashBufferTimer - Time.deltaTime);
        coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.deltaTime);
        wallJumpLockTimer = Mathf.Max(0f, wallJumpLockTimer - Time.deltaTime);
        ledgeClimbTimer = Mathf.Max(0f, ledgeClimbTimer - Time.deltaTime);
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);
        springLockTimer = Mathf.Max(0f, springLockTimer - Time.deltaTime);
        springGraceTimer = Mathf.Max(0f, springGraceTimer - Time.deltaTime);

        if (ledgeClimbTimer <= 0f) isLedgeClimbing = false;

        if (springGraceTimer > 0f && jumpPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + springJumpBoost);
            springGraceTimer = 0f;
            jumpBufferTimer = 0f;
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            currentStamina = maxStamina;
            if (dashCooldownTimer <= 0f) dashAvailable = true;
        }

        UpdateLowStaminaVisuals();
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        EvaluateCollision();

        if (isGrounded)
        {
            currentStamina = maxStamina;
            coyoteTimer = coyoteTime;
            if (dashCooldownTimer <= 0f) dashAvailable = true;
        }

        if (dashBufferTimer > 0f && dashAvailable && !isDashing)
        {
            PerformDash();
            dashBufferTimer = 0f;
        }

        if (isDashing)
        {
            dashAfterimageTimer -= Time.fixedDeltaTime;
            if (dashAfterimageTimer <= 0f)
            {
                SpawnDashAfterimage();
                dashAfterimageTimer = dashAfterimageInterval;
            }

            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                if (dashCooldown > 0f) dashCooldownTimer = dashCooldown;
                rb.gravityScale = 1f;

                if (dashDirection.y > 0f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * upwardDashEndMultiplier);
                }

                if (animator != null) animator.speed = 1f;
            }
            else
            {
                rb.linearVelocity = dashDirection * dashSpeed;
                return;
            }
        }

        if (jumpBufferTimer > 0f)
        {
            if (isGrounded || coyoteTimer > 0f)
            {
                PerformJump();
                jumpBufferTimer = 0f;
            }
            else if (isTouchingWall && !isGrounded && wallJumpLockTimer <= 0f)
            {
                PerformWallJump();
                jumpBufferTimer = 0f;
            }
        }

        bool canClimb = isTouchingWall && !isGrounded && climbHeld && currentStamina > 0f && wallJumpLockTimer <= 0f;
        if (canClimb)
        {
            isClimbing = true;
            isWallSliding = false;
            rb.gravityScale = 0f;

            float yVel = 0f;
            if (moveInput.y > 0.1f)
            {
                yVel = climbUpSpeed;
                currentStamina -= climbUpStaminaDrain * Time.fixedDeltaTime;

                if (enableLedgeClimb && CheckLedgeTopReached())
                {
                    PerformLedgeClimb();
                    return;
                }
            }
            else if (moveInput.y < -0.1f)
            {
                yVel = -climbDownSpeed;
                currentStamina -= climbDownStaminaDrain * Time.fixedDeltaTime;
            }
            else
            {
                yVel = 0f;
                currentStamina -= climbStaminaDrain * Time.fixedDeltaTime;
            }

            currentStamina = Mathf.Max(0f, currentStamina);
            rb.linearVelocity = new Vector2(wallDirection * 0.1f, yVel);
        }
        else
        {
            if (isClimbing)
            {
                isClimbing = false;
                rb.gravityScale = 1f;
            }
        }

        if (!isClimbing && !isLedgeClimbing)
        {
            if (springLockTimer > 0f) { }
            else if (wallJumpLockTimer <= 0f)
            {
                ApplyHorizontalMovement();
            }
            else
            {
                rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed), rb.linearVelocity.y);
            }

            if (isWallSliding)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
            }
            else
            {
                ApplyGravity();
            }

            if (enableCornerCorrection)
            {
                PerformUpwardCornerCorrection();
            }
        }
    }

    private void PerformUpwardCornerCorrection()
    {
        if (rb.linearVelocity.y <= 0f || boxCollider == null) return;

        Bounds bounds = boxCollider.bounds;
        Vector2 leftCorner = new Vector2(bounds.min.x + 0.02f, bounds.max.y);
        Vector2 rightCorner = new Vector2(bounds.max.x - 0.02f, bounds.max.y);

        bool leftHit = Physics2D.Raycast(leftCorner, Vector2.up, cornerCheckDistance, collisionMask);
        bool rightHit = Physics2D.Raycast(rightCorner, Vector2.up, cornerCheckDistance, collisionMask);

        if (rightHit && !leftHit)
        {
            transform.position += Vector3.left * cornerNudgeAmount;
        }
        else if (leftHit && !rightHit)
        {
            transform.position += Vector3.right * cornerNudgeAmount;
        }
    }

    private void UpdateLowStaminaVisuals()
    {
        if (spriteRenderer == null || dashFlashRoutine != null) return;

        float ratio = currentStamina / maxStamina;

        if (ratio <= lowStaminaThresholdRatio && currentStamina > 0f && (isClimbing || !isGrounded))
        {
            float flash = Mathf.PingPong(Time.time * lowStaminaBlinkSpeed, 1f);
            spriteRenderer.color = Color.Lerp(Color.white, lowStaminaFlashColor, flash);
        }
        else if (ratio <= 0f)
        {
            spriteRenderer.color = lowStaminaFlashColor;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    private bool CheckLedgeTopReached()
    {
        if (wallCheck == null) return false;

        Vector2 origin = (Vector2)wallCheck.position + Vector2.up * ledgeCheckHeight;
        Vector2 direction = Vector2.right * wallDirection;

        RaycastHit2D upperHit = Physics2D.Raycast(origin, direction, ledgeCheckForwardDistance, collisionMask);

        if (upperHit.collider == null)
        {
            Vector2 downOrigin = origin + (direction * ledgeCheckForwardDistance);
            RaycastHit2D downHit = Physics2D.Raycast(downOrigin, Vector2.down, ledgeCheckHeight, collisionMask);

            return downHit.collider != null;
        }

        return false;
    }

    private void PerformLedgeClimb()
    {
        isClimbing = false;
        isLedgeClimbing = true;
        ledgeClimbTimer = ledgeClimbLockDuration;

        rb.gravityScale = 1f;
        rb.linearVelocity = new Vector2(wallDirection * ledgeClimbForwardForce, ledgeClimbUpForce);
    }

    private void LateUpdate()
    {
        UpdateAnimation();
    }

    private void EvaluateCollision()
    {
        isGrounded = false;
        isTouchingWall = false;
        wallDirection = 0;
        isWallSliding = false;

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, collisionMask) != null;
        }

        if (wallCheck != null)
        {
            Vector2 position = wallCheck.position;
            Collider2D leftHit = Physics2D.OverlapCircle(position + Vector2.left * wallCheckRadius, wallCheckRadius, collisionMask);
            Collider2D rightHit = Physics2D.OverlapCircle(position + Vector2.right * wallCheckRadius, wallCheckRadius, collisionMask);

            if (leftHit != null)
            {
                isTouchingWall = true;
                wallDirection = -1;
            }
            else if (rightHit != null)
            {
                isTouchingWall = true;
                wallDirection = 1;
            }
        }

        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f && !isClimbing)
        {
            isWallSliding = true;
        }
    }

    private void ApplyHorizontalMovement()
    {
        float currentX = rb.linearVelocity.x;
        float targetX = moveInput.x * maxSpeed;

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            if (Mathf.Abs(currentX) > 0.01f && Mathf.Sign(moveInput.x) != Mathf.Sign(currentX))
            {
                currentX = 0f;
            }

            float accel = isGrounded ? groundAcceleration : airAcceleration;
            float maxDelta = accel * Time.fixedDeltaTime;
            float nextX = currentX + Mathf.Clamp(targetX - currentX, -maxDelta, maxDelta);
            rb.linearVelocity = new Vector2(nextX, rb.linearVelocity.y);
        }
        else
        {
            float decel = isGrounded ? groundDeceleration : airDeceleration;
            float maxDelta = decel * Time.fixedDeltaTime;
            float nextX = currentX + Mathf.Clamp(-currentX, -maxDelta, maxDelta);
            rb.linearVelocity = new Vector2(nextX, rb.linearVelocity.y);
        }

        if (Mathf.Abs(rb.linearVelocity.x) < 0.01f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed), rb.linearVelocity.y);
    }

    private void ApplyGravity()
    {
        float gravity = rb.linearVelocity.y < 0f ? fallGravity : riseGravity;
        if (Mathf.Abs(rb.linearVelocity.y) < 1f)
        {
            gravity = apexGravity;
        }

        float nextY = rb.linearVelocity.y - gravity * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(nextY, -maxFallSpeed));
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimer = 0f;
        isGrounded = false;
    }

    private void PerformWallJump()
    {
        if (currentStamina <= 0f)
        {
            if (!allowExhaustedWallJump) return;

            Vector2 weakJumpVelocity = new Vector2(wallJumpHorizontalForce * -wallDirection * 0.75f, 0f);
            rb.linearVelocity = weakJumpVelocity;
            wallJumpLockTimer = wallJumpLockTime;
            isGrounded = false;
            isWallSliding = false;
            isClimbing = false;
            return;
        }

        Vector2 wallJumpVelocity = new Vector2(wallJumpHorizontalForce * -wallDirection, wallJumpVerticalForce);
        rb.linearVelocity = wallJumpVelocity;

        currentStamina = Mathf.Max(0f, currentStamina - wallJumpStaminaCost);
        wallJumpLockTimer = wallJumpLockTime;
        // Wall jump no longer restores dash (Celeste rule)
        isGrounded = false;
        isWallSliding = false;
        isClimbing = false;
    }

    private void PerformDash()
    {
        if (!dashAvailable) return;

        dashDirection = Get8WayDashDirection();
        rb.linearVelocity = dashDirection * dashSpeed;

        dashTimer = dashDuration;
        isDashing = true;
        dashAvailable = false;
        isClimbing = false;
        rb.gravityScale = 0f;

        UpdateAnimation();

        if (animator != null) animator.speed = 0f;

        dashAfterimageTimer = dashAfterimageInterval;
        SpawnDashAfterimage();

        if (enableDashFlash) TriggerDashFlash();

        if (enableDashFreezeFrame)
        {
            if (freezeFrameRoutine != null) StopCoroutine(freezeFrameRoutine);
            freezeFrameRoutine = StartCoroutine(ApplyDashFreezeFrame());
        }
    }

    private Vector2 Get8WayDashDirection()
    {
        int x = 0;
        int y = 0;

        if (Mathf.Abs(moveInput.x) > 0.2f) x = moveInput.x > 0f ? 1 : -1;
        if (Mathf.Abs(moveInput.y) > 0.2f) y = moveInput.y > 0f ? 1 : -1;

        if (x == 0 && y == 0)
        {
            return new Vector2(facingDirection, 0f);
        }

        return new Vector2(x, y).normalized;
    }

    private IEnumerator ApplyDashFreezeFrame()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = dashFreezeFrameTimeScale;
        yield return new WaitForSecondsRealtime(dashFreezeFrameDuration);
        Time.timeScale = originalTimeScale;
        freezeFrameRoutine = null;
    }

    public void SpringLaunch(Vector2 launchVelocity, bool isHorizontal)
    {
        if (isDashing)
        {
            isDashing = false;
            rb.gravityScale = 1f;
            if (animator != null) animator.speed = 1f;
        }

        dashAvailable = true;
        dashCooldownTimer = 0f;
        currentStamina = maxStamina;

        Vector2 finalVelocity = launchVelocity;

        if ((jumpHeld || jumpBufferTimer > 0f) && launchVelocity.y > 0f)
        {
            finalVelocity.y += springJumpBoost;
            jumpBufferTimer = 0f;
        }
        else if (launchVelocity.y > 0f)
        {
            springGraceTimer = springGraceTime;
        }

        rb.linearVelocity = finalVelocity;

        if (finalVelocity.x != 0f)
        {
            facingDirection = finalVelocity.x > 0f ? 1 : -1;
        }

        springLockTimer = isHorizontal ? horizontalSpringLockTime : 0f;
    }

    private void UpdateAnimation()
    {
        UpdateFacing();

        if (isDead)
        {
            PlayAnimation(dashAvailable ? animDeath : animDeathNoDash);
            return;
        }

        // Plays climb state whenever player touches a wall airborne, regardless of grab key input
        if (!isGrounded && isTouchingWall)
        {
            PlayAnimation(dashAvailable ? animClimb : animClimbNoDash);
        }
        else if (!isGrounded)
        {
            bool isRising = rb.linearVelocity.y > 0.05f;
            if (isRising)
            {
                PlayAnimation(dashAvailable ? animJump : animJumpNoDash);
            }
            else
            {
                PlayAnimation(dashAvailable ? animFall : animFallNoDash);
            }
        }
        else if (Mathf.Abs(rb.linearVelocity.x) > runAnimSpeedThreshold)
        {
            PlayAnimation(dashAvailable ? animRun : animRunNoDash);
        }
        else
        {
            PlayAnimation(dashAvailable ? animIdle : animIdleNoDash);
        }
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null) return;
        bool faceLeft = facingDirection < 0;
        spriteRenderer.flipX = spriteFacesRightByDefault ? faceLeft : !faceLeft;
    }

    private void PlayAnimation(int animationHash)
    {
        if (animator == null || currentAnimHash == animationHash) return;
        if (!animator.HasState(0, animationHash)) return;

        currentAnimHash = animationHash;
        animator.CrossFadeInFixedTime(animationHash, 0.05f, 0);

        if (animator.speed == 0f)
        {
            animator.Update(0f);
        }
    }

    private void SpawnDashAfterimage()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        GameObject ghost = new GameObject("DashAfterimage");
        ghost.transform.SetPositionAndRotation(spriteRenderer.transform.position, spriteRenderer.transform.rotation);
        ghost.transform.localScale = spriteRenderer.transform.lossyScale;

        SpriteRenderer ghostRenderer = ghost.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = spriteRenderer.sprite;
        ghostRenderer.flipX = spriteRenderer.flipX;
        ghostRenderer.flipY = spriteRenderer.flipY;
        ghostRenderer.color = dashAfterimageColor;
        ghostRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = spriteRenderer.sortingOrder + dashAfterimageSortingOrderOffset;
        if (spriteRenderer.sharedMaterial != null) ghostRenderer.sharedMaterial = spriteRenderer.sharedMaterial;

        StartCoroutine(FadeAndDestroyAfterimage(ghostRenderer));
    }

    private IEnumerator FadeAndDestroyAfterimage(SpriteRenderer ghostRenderer)
    {
        float elapsed = 0f;
        Color startColor = ghostRenderer.color;

        while (elapsed < dashAfterimageFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dashAfterimageFadeDuration);
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            ghostRenderer.color = c;
            yield return null;
        }

        Destroy(ghostRenderer.gameObject);
    }

    private void TriggerDashFlash()
    {
        if (spriteRenderer == null) return;
        if (dashFlashRoutine != null) StopCoroutine(dashFlashRoutine);
        dashFlashRoutine = StartCoroutine(DashFlashRoutine());
    }

    private IEnumerator DashFlashRoutine()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = dashFlashColor;
        yield return new WaitForSeconds(dashFlashDuration);
        spriteRenderer.color = original;
        dashFlashRoutine = null;
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        isDashing = false;
        isWallSliding = false;
        isClimbing = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (animator != null) animator.speed = 1f;
    }

    public void Respawn()
    {
        isDead = false;
        dashAvailable = true;
        dashCooldownTimer = 0f;
        currentStamina = maxStamina;
        currentAnimHash = 0;

        rb.gravityScale = 1f;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator RespawnSequence()
    {
        Die();
        yield return new WaitForSeconds(respawnDelay);

        transform.position = respawnPoint != null ? respawnPoint.position : startPosition;
        Respawn();
    }

    private void OnTriggerEnter2D(Collider2D other) => EvaluateHazardContact(other.gameObject);
    private void OnCollisionEnter2D(Collision2D other) => EvaluateHazardContact(other.gameObject);

    private void EvaluateHazardContact(GameObject contactedObject)
    {
        if (isDead) return;

        if (((1 << contactedObject.layer) & damageLayer) != 0)
        {
            StartCoroutine(RespawnSequence());
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.green;
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.cyan;
        if (wallCheck != null)
        {
            Gizmos.DrawWireSphere(wallCheck.position + Vector3.left * wallCheckRadius, wallCheckRadius);
            Gizmos.DrawWireSphere(wallCheck.position + Vector3.right * wallCheckRadius, wallCheckRadius);

            Vector2 origin = (Vector2)wallCheck.position + Vector2.up * ledgeCheckHeight;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(origin, Vector3.right * wallDirection * ledgeCheckForwardDistance);
        }

        if (boxCollider != null && enableCornerCorrection)
        {
            Gizmos.color = Color.yellow;
            Bounds bounds = boxCollider.bounds;
            Vector2 leftCorner = new Vector2(bounds.min.x + 0.02f, bounds.max.y);
            Vector2 rightCorner = new Vector2(bounds.max.x - 0.02f, bounds.max.y);

            Gizmos.DrawRay(leftCorner, Vector2.up * cornerCheckDistance);
            Gizmos.DrawRay(rightCorner, Vector2.up * cornerCheckDistance);
        }
    }
}