using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Move")]
    public float detectRange = 8f;
    public float moveSpeed = 1.6f;
    public float stopDistance = 3f;
    public bool invertFacing = false;

    [Header("Dash Attack")]
    public float dashSpeed = 10f;
    public float dashDuration = 0.25f;
    public float dashPrepareTime = 0.45f;

    [Header("Axe Attack")]
    public Transform axeFirePoint;
    public GameObject axeProjectilePrefab;
    public int axeDamage = 1;
    public float axeSpeed = 7f;

    [Header("Timing")]
    public float attackCooldown = 1.4f;

    private Rigidbody2D rb;
    private float nextAttackTime;
    private bool isAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.identity;

        if (target == null)
        {
            StopMoving();
            return;
        }

        if (isAttacking)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > detectRange)
        {
            StopMoving();
            return;
        }

        FaceTarget();

        if (Time.time >= nextAttackTime)
        {
            StartCoroutine(ChooseAttack(distance));
            return;
        }

        if (distance > stopDistance)
        {
            MoveTowardTarget();
        }
        else
        {
            StopMoving();
        }
    }

    private IEnumerator ChooseAttack(float distance)
    {
        isAttacking = true;
        StopMoving();

        if (distance <= stopDistance + 1f)
        {
            yield return DashAttack();
        }
        else
        {
            yield return AxeAttack();
        }

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    private IEnumerator DashAttack()
    {
        yield return new WaitForSeconds(dashPrepareTime);

        float direction = target.position.x > transform.position.x ? 1f : -1f;
        float timer = dashDuration;

        while (timer > 0f)
        {
            rb.linearVelocity = new Vector2(direction * dashSpeed, rb.linearVelocity.y);
            timer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        StopMoving();
    }

    private IEnumerator AxeAttack()
    {
        yield return new WaitForSeconds(0.25f);

        if (axeFirePoint != null && axeProjectilePrefab != null)
        {
            Vector2 direction = target.position.x > axeFirePoint.position.x ? Vector2.right : Vector2.left;
            GameObject axeObject = Instantiate(axeProjectilePrefab, axeFirePoint.position, Quaternion.identity);

            BossAxeProjectile axe = axeObject.GetComponent<BossAxeProjectile>();
            if (axe != null)
            {
                axe.Launch(direction, axeDamage, axeSpeed);
            }
        }
    }

    private void MoveTowardTarget()
    {
        float direction = target.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    private void FaceTarget()
    {
        float direction = target.position.x > transform.position.x ? 1f : -1f;
        if (invertFacing) direction *= -1f;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
}
