using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 2f;
    public bool startMovingRight = false;

    [Header("Checks")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.7f;
    public Transform wallCheck;
    public float wallCheckDistance = 0.4f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool movingRight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movingRight = startMovingRight;
    }

    private void FixedUpdate()
    {
        bool hasGroundAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        Vector2 wallDir = movingRight ? Vector2.right : Vector2.left;
        bool hitsWall = Physics2D.Raycast(wallCheck.position, wallDir, wallCheckDistance, groundLayer);

        if (!hasGroundAhead || hitsWall)
        {
            Flip();
        }

        float dir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    private void Flip()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;
        scale.x = -scale.x;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 dir = transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * wallCheckDistance);
        }
    }
}
