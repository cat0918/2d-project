using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMove), typeof(Rigidbody2D))]
public class PlayerJumpPose : MonoBehaviour
{
    [Header("SPUM Arms (Auto Assigned)")]
    public Transform leftArm;
    public Transform rightArm;

    [Header("Jump Pose")]
    [Min(0f)] public float risingArmLift = 0.04f;
    [Min(0f)] public float fallingArmLift = 0.02f;
    public float risingArmAngle = 140f;
    public float fallingArmAngle = 70f;
    [Min(0.1f)] public float blendSpeed = 14f;

    private PlayerMove movement;
    private Rigidbody2D body;
    private PlayerAttack attack;
    private PlayerAnimation animationDriver;
    private Vector3 leftPosition;
    private Vector3 rightPosition;
    private Quaternion leftRotation;
    private Quaternion rightRotation;
    private float poseWeight;
    private float lift;
    private float angle;
    private bool ready;
    private bool applied;
    private Vector3 animatedLeftPosition;
    private Vector3 animatedRightPosition;
    private Quaternion animatedLeftRotation;
    private Quaternion animatedRightRotation;

    private void Awake()
    {
        movement = GetComponent<PlayerMove>();
        body = GetComponent<Rigidbody2D>();
        attack = GetComponent<PlayerAttack>();
        animationDriver = GetComponent<PlayerAnimation>();
        SPUM_Prefabs spum = GetComponentInChildren<SPUM_Prefabs>();
        if (spum != null && spum._anim != null)
        {
            Transform root = spum._anim.transform;
            if (leftArm == null) leftArm = root.Find("Root/BodySet/P_Body/ArmSet/ArmL");
            if (rightArm == null) rightArm = root.Find("Root/BodySet/P_Body/ArmSet/ArmR");
        }

        if (leftArm == null || rightArm == null)
        {
            Debug.LogWarning("PlayerJumpPose needs SPUM Root/BodySet/P_Body/ArmSet/ArmL and ArmR.", this);
            enabled = false;
            return;
        }

        leftPosition = leftArm.localPosition;
        rightPosition = rightArm.localPosition;
        leftRotation = leftArm.localRotation;
        rightRotation = rightArm.localRotation;
        ready = true;
    }

    private void Update()
    {
        // Remove last frame's overlay before Animator evaluates new animation.
        RestoreAnimatedPose();
    }

    private void LateUpdate()
    {
        if (!ready) return;
        // Let the Animator fully control the weapon during an attack.
        if ((attack != null && attack.IsAttacking) ||
            (animationDriver != null && animationDriver.IsReactingToHit))
        {
            poseWeight = 0f;
            return;
        }
        bool grounded = movement.groundCheck != null &&
            Physics2D.OverlapCircle(movement.groundCheck.position,
                movement.groundCheckRadius, movement.groundLayer) != null;
        bool airborne = movement.isActiveAndEnabled && movement.groundCheck != null &&
            (!grounded || body.linearVelocity.y > 0.1f);
        bool rising = body.linearVelocity.y > 0.1f;
        float blend = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        poseWeight = Mathf.Lerp(poseWeight, airborne ? 1f : 0f, blend);
        lift = Mathf.Lerp(lift, rising ? risingArmLift : fallingArmLift, blend);
        angle = Mathf.Lerp(angle, rising ? risingArmAngle : fallingArmAngle, blend);

        if (!airborne && poseWeight < 0.01f)
        {
            poseWeight = 0f;
            return;
        }

        // Animator has evaluated by LateUpdate; blend over its current pose.
        animatedLeftPosition = leftArm.localPosition;
        animatedRightPosition = rightArm.localPosition;
        animatedLeftRotation = leftArm.localRotation;
        animatedRightRotation = rightArm.localRotation;
        leftArm.localPosition = Vector3.Lerp(leftArm.localPosition,
            leftPosition + Vector3.up * lift, poseWeight);
        rightArm.localPosition = Vector3.Lerp(rightArm.localPosition,
            rightPosition + Vector3.up * lift, poseWeight);
        leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation,
            leftRotation * Quaternion.Euler(0f, 0f, angle), poseWeight);
        rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation,
            rightRotation * Quaternion.Euler(0f, 0f, -angle), poseWeight);
        applied = true;
    }

    private void RestoreAnimatedPose()
    {
        if (ready && applied)
        {
            if (leftArm != null)
            {
                leftArm.localPosition = animatedLeftPosition;
                leftArm.localRotation = animatedLeftRotation;
            }
            if (rightArm != null)
            {
                rightArm.localPosition = animatedRightPosition;
                rightArm.localRotation = animatedRightRotation;
            }
        }
        applied = false;
    }

    private void OnDisable()
    {
        RestoreAnimatedPose();
        poseWeight = 0f;
        lift = 0f;
        angle = 0f;
    }
}
