using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;
    public int maxAirJumps = 1;
    [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f;
    public bool invertFacing = false;

    [Header("Sound Effects")]
    public bool soundEffectsEnabled = true;
    [Range(0f, 1f)] public float soundEffectsVolume = 0.4f;
    [Range(0f, 1f)] public float footstepVolume = 0.7f;
    [Min(0.2f)] public float footstepDistance = 1.35f;
    private Vector2 lastFootstepPosition;
    private float walkedDistance;
    private bool footstepInitialized;
    private bool footstepWasGrounded;

    [Header("Abilities")]
    public bool startWithDoubleJump = true;
    public bool HasDoubleJump { get; private set; }

    [Header("Dash")]
    public float dashSpeed = 16f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 0.5f;
    public bool dashEffectsEnabled = true;
    [Range(0.3f, 2f)] public float dashEffectScale = 1f;
    public Color dashEffectColor = new Color(0.4f, 0.85f, 0.95f, 0.8f);
    private PlayerDashEffect dashEffect;

    public float DashRecharge01 => isDashing ? 0f :
        (dashCooldown <= 0f ? 1f : Mathf.Clamp01(1f - dashCooldownLeft / dashCooldown));
    public bool IsDashReady => isActiveAndEnabled && !isDashing && dashCooldownLeft <= 0f && Time.time >= knockbackEnds;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Jump And Landing Feedback")]
    public bool movementEffectsEnabled = true;
    [Range(0.3f, 2f)] public float movementEffectScale = 1f;
    public Color dustColor = new Color(0.7f, 0.75f, 0.78f, 0.7f);

    private Collider2D bodyCollider;
    private bool feedbackInitialized;
    private float feedbackAirTime;
    private Vector2 feedbackLastPosition;

    private Rigidbody2D rb;
    private float moveInput;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool dashPressed;
    private bool isGrounded;
    private int airJumpCount;
    private bool isDashing;
    private float dashTimeLeft;
    private float dashCooldownLeft;
    private float dashDirection = 1f;
    private float defaultGravityScale;
    private float knockbackEnds;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        defaultGravityScale = rb.gravityScale;
        HasDoubleJump = startWithDoubleJump;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || GameFlowScreen.BlocksInput)
        {
            ClearMovementInput();
            return;
        }
        if (Time.time < knockbackEnds)
        {
            ClearMovementInput();
            return;
        }
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
        }
        if (Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space))
        {
            jumpReleased = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            dashPressed = true;
        }

        if (moveInput != 0f)
        {
            Vector3 scale = transform.localScale;
            float facingSign = invertFacing ? -Mathf.Sign(moveInput) : Mathf.Sign(moveInput);
            scale.x = facingSign * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void FixedUpdate()
    {
        UpdateFootsteps();
        UpdateLandingFeedback();
        float dt = Time.fixedDeltaTime;
        if (dashCooldownLeft > 0f)
        {
            dashCooldownLeft -= dt;
        }

        if (Time.time < knockbackEnds)
        {
            ClearMovementInput();
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            airJumpCount = 0;
        }

        if (isDashing)
        {
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            dashTimeLeft -= dt;

            if (dashTimeLeft <= 0f)
            {
                EndDash();
            }

            jumpPressed = false;
            jumpReleased = false;
            dashPressed = false;
            return;
        }

        if (dashPressed && dashCooldownLeft <= 0f)
        {
            StartDash();
            jumpPressed = false;
            jumpReleased = false;
            dashPressed = false;
            return;
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (jumpPressed)
        {
            if (isGrounded && rb.linearVelocity.y <= 0f)
            {
                GameSfx.Play(GameSound.Jump, transform.position);
                PlayMovementEffect(false);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (HasDoubleJump && airJumpCount < maxAirJumps)
            {
                GameSfx.Play(GameSound.Jump, transform.position);
                PlayMovementEffect(false, true);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                airJumpCount++;
            }
        }

        if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        jumpPressed = false;
        jumpReleased = false;
        dashPressed = false;
    }

    public void BeginKnockback(float duration)
    {
        if (isDashing) EndDash();
        knockbackEnds = Time.time + Mathf.Max(0f, duration);
        ClearMovementInput();
    }

    private void UpdateLandingFeedback()
    {
        Vector2 position = rb.position;
        if (groundCheck == null) return;
        // Ignore initialization and teleports, including checkpoint respawns.
        if (!feedbackInitialized || Vector2.Distance(position, feedbackLastPosition) > 2f)
        {
            feedbackInitialized = true;
            feedbackLastPosition = position;
            feedbackAirTime = 0f;
            return;
        }
        feedbackLastPosition = position;
        bool touchingGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (!touchingGround)
            feedbackAirTime += Time.fixedDeltaTime;
        else if (rb.linearVelocity.y <= 0.1f)
        {
            if (feedbackAirTime >= 0.08f)
            {
                PlayMovementEffect(true);
                GameSfx.Play(GameSound.Land, transform.position);
            }
            feedbackAirTime = 0f;
        }
    }

    private void PlayMovementEffect(bool landing, bool airJump = false)
    {
        if (!movementEffectsEnabled) return;
        Vector2 feet = bodyCollider != null
            ? new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y + 0.04f)
            : (Vector2)groundCheck.position;
        MovementDustBurst.Play(feet, landing, airJump, movementEffectScale, dustColor);
    }

    private void OnDisable()
    {
        footstepInitialized = false;
        footstepWasGrounded = false;
        walkedDistance = 0f;
        if (isDashing) EndDash();
        else if (dashEffect != null) { dashEffect.StopEffect(); dashEffect = null; }
        feedbackInitialized = false;
        feedbackAirTime = 0f;
    }

    private void UpdateFootsteps()
    {
        Vector2 position = rb.position;
        Vector2 displacement = position - lastFootstepPosition;
        lastFootstepPosition = position;
        bool grounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool canStep = footstepInitialized && footstepWasGrounded && grounded &&
            !isDashing && !dashPressed && !jumpPressed && Time.time >= knockbackEnds &&
            Mathf.Abs(moveInput) > 0.1f && Mathf.Abs(rb.linearVelocity.y) < 0.2f &&
            displacement.magnitude < Mathf.Abs(moveSpeed) * Time.fixedDeltaTime * 2f + 0.1f;
        footstepInitialized = true;
        footstepWasGrounded = grounded;
        if (!canStep || Mathf.Abs(displacement.x) < 0.001f)
        {
            walkedDistance = 0f;
            return;
        }
        walkedDistance += Mathf.Abs(displacement.x);
        if (walkedDistance >= Mathf.Max(0.2f, footstepDistance))
        {
            walkedDistance %= Mathf.Max(0.2f, footstepDistance);
            GameSfx.PlayFootstep(transform.position, footstepVolume);
        }
    }

    private void ClearMovementInput()
    {
        moveInput = 0f;
        jumpPressed = false;
        jumpReleased = false;
        dashPressed = false;
    }

    public void UnlockDoubleJump()
    {
        HasDoubleJump = true;
        maxAirJumps = Mathf.Max(1, maxAirJumps);
    }

    private void StartDash()
    {
        GameSfx.Play(GameSound.Dash, transform.position);
        isDashing = true;
        dashTimeLeft = dashDuration;
        dashCooldownLeft = dashCooldown;
        rb.gravityScale = 0f;

        if (moveInput != 0f)
        {
            dashDirection = Mathf.Sign(moveInput);
        }
        else
        {
            dashDirection = Mathf.Sign(transform.localScale.x);
            if (dashDirection == 0f) dashDirection = 1f;
        }
        if (dashEffectsEnabled)
        {
            if (dashEffect != null) dashEffect.StopEffect();
            dashEffect = PlayerDashEffect.Play(this, bodyCollider, dashDirection, dashEffectScale, dashEffectColor);
            if (isGrounded) PlayMovementEffect(false);
        }
    }

    private void EndDash()
    {
        if (dashEffect != null) dashEffect.StopEffect();
        dashEffect = null;
        isDashing = false;
        rb.gravityScale = defaultGravityScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
