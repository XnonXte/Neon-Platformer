using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Acceleration while grounded. Higher values feel snappier.")]
    [SerializeField] private float groundAcceleration = 120f;

    [Tooltip("Acceleration while airborne.")]
    [SerializeField] private float airAcceleration = 80f;

    [Tooltip("Deceleration while grounded. High values mimic Hollow Knight's instant stop.")]
    [SerializeField] private float groundDeceleration = 140f;

    [Tooltip("Deceleration while airborne.")]
    [SerializeField] private float airDeceleration = 60f;

    [Tooltip("Maximum horizontal speed.")]
    [SerializeField] private float maxSpeed = 10f;

    [Header("Jump")]
    [Tooltip("Impulse applied on a jump.")]
    [SerializeField] private float jumpForce = 16.5f;

    [Tooltip("Multiplier applied to upward linearVelocity when jump is released early.")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Tooltip("How long the player can still jump after leaving the ground.")]
    [SerializeField] private float coyoteTime = 0.1f;

    [Tooltip("How long jump input is remembered before landing.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Tooltip("How long dash input is remembered before landing or becoming available.")]
    [SerializeField] private float dashBufferTime = 0.1f;

    [Header("Gravity")]
    [Tooltip("Gravity while rising.")]
    [SerializeField] private float riseGravity = 40f;

    [Tooltip("Gravity while falling. Hollow Knight features heavy, rapid falls.")]
    [SerializeField] private float fallGravity = 65f;

    [Tooltip("Lower gravity near the jump apex to make motion feel smoother.")]
    [SerializeField] private float apexGravity = 28f;

    [Tooltip("Maximum downward speed.")]
    [SerializeField] private float maxFallSpeed = 28f;

    [Header("Wall")]
    [Tooltip("Wall slide speed when sliding down a wall.")]
    [SerializeField] private float wallSlideSpeed = 4f;

    [Tooltip("Horizontal force applied when wall jumping.")]
    [SerializeField] private float wallJumpHorizontalForce = 12f;

    [Tooltip("Vertical force applied when wall jumping.")]
    [SerializeField] private float wallJumpVerticalForce = 16.5f;

    [Tooltip("How long the player is locked out from wall jumping control.")]
    [SerializeField] private float wallJumpLockTime = 0.15f;

    [Header("Dash")]
    [Tooltip("Dash speed. 24-26 provides a highly accurate, crisp response.")]
    [SerializeField] private float dashSpeed = 24f;

    [Tooltip("Speed when executing a downward plunge dash.")]
    [SerializeField] private float downwardDashSpeed = 32f;

    [Tooltip("How long the dash lasts.")]
    [SerializeField] private float dashDuration = 0.15f;

    [Tooltip("Optional cooldown between dashes. Leave at zero for no cooldown.")]
    [SerializeField] private float dashCooldown = 0.2f;

    [Tooltip("Optional brief freeze frame when dashing. Leave disabled for a more classic feel.")]
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
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;

    [Header("Death and Respawn")]
    [Tooltip("The Layer representing hazards/damage (e.g., Spikes, Acid, Void).")]
    [SerializeField] private LayerMask damageLayer;
    [Tooltip("Fixed transform point where the player will respawn.")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("How many seconds to wait in the death state before reviving.")]
    [SerializeField] private float respawnDelay = 1.2f;

    [Header("Animation")]
    [Tooltip("Animator driving the character clips (idle, run, jump, fall, death).")]
    [SerializeField] private Animator animator;

    [Tooltip("SpriteRenderer used for facing-direction flip and dash afterimages.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Enable if the source sprite faces right by default. Disable if it faces left by default.")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    [Tooltip("Minimum horizontal speed to be considered 'running' for animation purposes.")]
    [SerializeField] private float runAnimSpeedThreshold = 0.1f;

    [Header("Dash VFX (Celeste-style afterimages)")]
    [Tooltip("Tint applied to spawned afterimage ghosts.")]
    [SerializeField] private Color dashAfterimageColor = new Color(1f, 1f, 1f, 0.55f);

    [Tooltip("Seconds between each afterimage spawn while dashing.")]
    [SerializeField] private float dashAfterimageInterval = 0.02f;

    [Tooltip("How long each afterimage takes to fade out.")]
    [SerializeField] private float dashAfterimageFadeDuration = 0.25f;

    [Tooltip("Sorting order offset applied to afterimages, relative to the player sprite.")]
    [SerializeField] private int dashAfterimageSortingOrderOffset = -1;

    [Tooltip("Enable brief flash tint applied to the sprite the instant a dash begins.")]
    [SerializeField] private bool enableDashFlash = true;

    [Tooltip("Color used for the dash-start flash.")]
    [SerializeField] private Color dashFlashColor = Color.white;

    [Tooltip("Duration of the dash-start flash.")]
    [SerializeField] private float dashFlashDuration = 0.06f;

    [Header("Debug")]
    [Tooltip("Shows the collision checks in the Scene view.")]
    [SerializeField] private bool showDebugGizmos = true;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool jumpReleased;
    private bool dashPressed;

    private bool isGrounded;
    private bool isTouchingWall;
    private int wallDirection;
    private bool isWallSliding;

    // Dash Tracking States
    private bool isDashing;
    private bool isDownwardDash;
    private bool dashAvailable = true;
    private int facingDirection = 1;

    private float jumpBufferTimer;
    private float dashBufferTimer;
    private float coyoteTimer;
    private float wallJumpLockTimer;
    private float dashCooldownTimer;
    private float dashTimer;

    private Coroutine freezeFrameRoutine;

    // Animation / VFX state
    private bool isDead;
    private float dashAfterimageTimer;
    private int currentAnimHash;
    private Coroutine dashFlashRoutine;

    // Respawn Fallback
    private Vector2 startPosition;

    private static readonly int AnimIdle = Animator.StringToHash("idle");
    private static readonly int AnimRun = Animator.StringToHash("run");
    private static readonly int AnimJump = Animator.StringToHash("jump");
    private static readonly int AnimFall = Animator.StringToHash("fall");
    private static readonly int AnimDeath = Animator.StringToHash("death");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Cache start position in case no respawn transform is assigned in the inspector
        startPosition = transform.position;
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null) moveAction.action.Enable();
        if (jumpAction != null && jumpAction.action != null) jumpAction.action.Enable();
        if (dashAction != null && dashAction.action != null) dashAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null) moveAction.action.Disable();
        if (jumpAction != null && jumpAction.action != null) jumpAction.action.Disable();
        if (dashAction != null && dashAction.action != null) dashAction.action.Disable();
    }

    private void Update()
    {
        if (isDead) return;

        moveInput = GetMoveInput();
        jumpPressed = GetJumpPressed();
        jumpHeld = GetJumpHeld();
        jumpReleased = GetJumpReleased();
        dashPressed = GetDashPressed();

        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }

        if (dashPressed)
        {
            dashBufferTimer = dashBufferTime;
        }

        if (jumpReleased && !isGrounded && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        if (moveInput.x != 0f)
        {
            facingDirection = moveInput.x > 0f ? 1 : -1;
        }

        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);
        dashBufferTimer = Mathf.Max(0f, dashBufferTimer - Time.deltaTime);
        coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.deltaTime);
        wallJumpLockTimer = Mathf.Max(0f, wallJumpLockTimer - Time.deltaTime);
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (dashCooldownTimer <= 0f)
            {
                dashAvailable = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            // Fully freeze velocity during death to prevent sliding off platforms
            rb.linearVelocity = Vector2.zero;
            return;
        }

        EvaluateCollision();

        // Downward Dash Ground-Cancel
        if (isDashing && isDownwardDash && isGrounded)
        {
            isDashing = false;
            isDownwardDash = false;
            if (dashCooldown > 0f)
            {
                dashCooldownTimer = dashCooldown;
            }
            rb.gravityScale = 1f;
            if (animator != null) animator.speed = 1f;
        }

        // 1. Ignite Dash immediately
        if (dashBufferTimer > 0f && dashAvailable && !isDashing)
        {
            PerformDash();
            dashBufferTimer = 0f;
        }

        // 2. Track ongoing dash physics execution loop
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
                isDownwardDash = false;
                if (dashCooldown > 0f)
                {
                    dashCooldownTimer = dashCooldown;
                }
                rb.gravityScale = 1f;
                if (animator != null) animator.speed = 1f;
            }
            else
            {
                if (isDownwardDash)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                }
                return;
            }
        }

        // 3. Grounded / Aerial Replenish Rules
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (dashCooldownTimer <= 0f)
            {
                dashAvailable = true;
            }
        }

        // 4. Input Buffer Logic
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

        // 5. Environmental Movement Adjustments
        if (wallJumpLockTimer <= 0f)
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

        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f)
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
        Vector2 wallJumpVelocity = new Vector2(wallJumpHorizontalForce * -wallDirection, wallJumpVerticalForce);
        rb.linearVelocity = wallJumpVelocity;

        wallJumpLockTimer = wallJumpLockTime;
        dashAvailable = true;
        isGrounded = false;
        isWallSliding = false;
    }

    private void PerformDash()
    {
        if (!dashAvailable) return;

        isDownwardDash = (moveInput.y < -0.5f && !isGrounded);

        if (isDownwardDash)
        {
            rb.linearVelocity = Vector2.down * downwardDashSpeed;
        }
        else
        {
            Vector2 dashDirection = GetDashDirection();
            rb.linearVelocity = dashDirection * dashSpeed;
        }

        dashTimer = dashDuration;
        isDashing = true;
        dashAvailable = false;
        rb.gravityScale = 0f;

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

    private Vector2 GetDashDirection()
    {
        float horizontalInput = moveInput.x;
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            return horizontalInput > 0f ? Vector2.right : Vector2.left;
        }
        return facingDirection == 1 ? Vector2.right : Vector2.left;
    }

    private System.Collections.IEnumerator ApplyDashFreezeFrame()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = dashFreezeFrameTimeScale;
        yield return new WaitForSecondsRealtime(dashFreezeFrameDuration);
        Time.timeScale = originalTimeScale;
        freezeFrameRoutine = null;
    }

    // ---------------------------------------------------------------------
    // Animation & facing
    // ---------------------------------------------------------------------

    private void UpdateAnimation()
    {
        UpdateFacing();

        if (isDead)
        {
            PlayAnimation(AnimDeath);
            return;
        }

        if (isDashing)
        {
            return;
        }

        if (!isGrounded)
        {
            PlayAnimation(rb.linearVelocity.y > 0.05f ? AnimJump : AnimFall);
        }
        else if (Mathf.Abs(rb.linearVelocity.x) > runAnimSpeedThreshold)
        {
            PlayAnimation(AnimRun);
        }
        else
        {
            PlayAnimation(AnimIdle);
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
        currentAnimHash = animationHash;
        animator.CrossFadeInFixedTime(animationHash, 0.05f, 0);
    }

    // ---------------------------------------------------------------------
    // Dash VFX
    // ---------------------------------------------------------------------

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

    // ---------------------------------------------------------------------
    // Death / respawn
    // ---------------------------------------------------------------------

    /// <summary>Call from your health/hazard system when the player should die.</summary>
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        isDashing = false;
        isDownwardDash = false;
        isWallSliding = false;

        rb.gravityScale = 0f; // Prevent gravity from dropping the corpse off-screen during death animation
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // Disable physics detection & triggers during death

        if (animator != null) animator.speed = 1f;
    }

    /// <summary>Call after repositioning the player to bring input/physics back online.</summary>
    public void Respawn()
    {
        isDead = false;
        dashAvailable = true;
        dashCooldownTimer = 0f;
        currentAnimHash = 0;

        rb.gravityScale = 1f;
        rb.simulated = true; // Turn physics/triggers back on
        rb.linearVelocity = Vector2.zero;
    }

    // Coroutine sequence to transition death to respawn smoothly
    private IEnumerator RespawnSequence()
    {
        Die();

        yield return new WaitForSeconds(respawnDelay);

        // Reposition the player
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
        }
        else
        {
            transform.position = startPosition;
        }

        Respawn();
    }

    // ---------------------------------------------------------------------
    // Hazard Collision & Trigger Detections
    // ---------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        EvaluateHazardContact(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        EvaluateHazardContact(other.gameObject);
    }

    private void EvaluateHazardContact(GameObject contactedObject)
    {
        if (isDead) return;

        // Check if the layer of the contacted object exists within the designated damage LayerMask
        if (((1 << contactedObject.layer) & damageLayer) != 0)
        {
            StartCoroutine(RespawnSequence());
        }
    }

    // ---------------------------------------------------------------------
    // Input Handling
    // ---------------------------------------------------------------------

    private Vector2 GetMoveInput()
    {
        if (moveAction != null && moveAction.action != null) return moveAction.action.ReadValue<Vector2>();

        Vector2 axis = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) axis.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) axis.x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) axis.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) axis.y -= 1f;
        }
        if (Gamepad.current != null) axis += Gamepad.current.leftStick.ReadValue();

        return Vector2.ClampMagnitude(axis, 1f);
    }

    private bool GetJumpPressed()
    {
        if (jumpAction != null && jumpAction.action != null) return jumpAction.action.WasPressedThisFrame();
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private bool GetJumpHeld()
    {
        if (jumpAction != null && jumpAction.action != null) return jumpAction.action.IsPressed();
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
    }

    private bool GetJumpReleased()
    {
        if (jumpAction != null && jumpAction.action != null) return jumpAction.action.WasReleasedThisFrame();
        return Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame;
    }

    private bool GetDashPressed()
    {
        if (dashAction != null && dashAction.action != null) return dashAction.action.WasPressedThisFrame();
        return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
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
        }
    }
}