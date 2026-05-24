using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;
    public int maxAirJumps = 1;
    [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("Dash")]
    public float dashSpeed = 16f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
    }

    private void Update()
    {
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
            scale.x = Mathf.Sign(moveInput) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (dashCooldownLeft > 0f)
        {
            dashCooldownLeft -= dt;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)
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
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (airJumpCount < maxAirJumps)
            {
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

    private void StartDash()
    {
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
    }

    private void EndDash()
    {
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
