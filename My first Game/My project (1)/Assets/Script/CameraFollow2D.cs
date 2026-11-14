using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(0f, 1f);
    [Min(0.01f)] public float smoothTime = 0.2f;
    public bool snapOnStart = true;
    public bool freezeOnDeath = true;

    [Header("Optional Map Bounds (World Coordinates)")]
    public bool useBounds;
    public Vector2 minBounds = new Vector2(-20f, -10f);
    public Vector2 maxBounds = new Vector2(20f, 10f);

    private Camera view;
    private Vector2 velocity;
    private float fixedZ;

    private void Awake()
    {
        view = GetComponent<Camera>();
        fixedZ = transform.position.z;
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("Assign PlayerNew to CameraFollow2D Target.", this);
            return;
        }
        if (snapOnStart) SetPosition(ClampToMap((Vector2)target.position + offset));
    }

    private void OnEnable()
    {
        velocity = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            velocity = Vector2.zero;
            return;
        }
        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (freezeOnDeath && health != null && health.IsDead)
        {
            velocity = Vector2.zero;
            return;
        }

        Vector2 desired = ClampToMap((Vector2)target.position + offset);
        Vector2 next = Vector2.SmoothDamp(transform.position, desired, ref velocity,
            Mathf.Max(0.01f, smoothTime), Mathf.Infinity, Time.deltaTime);
        SetPosition(ClampToMap(next));
    }

    private Vector2 ClampToMap(Vector2 position)
    {
        if (!useBounds || !view.orthographic) return position;
        Vector2 min = Vector2.Min(minBounds, maxBounds);
        Vector2 max = Vector2.Max(minBounds, maxBounds);
        float halfHeight = view.orthographicSize;
        float halfWidth = halfHeight * view.aspect;
        // A map smaller than the viewport is centered instead of using inverted limits.
        position.x = max.x - min.x <= halfWidth * 2f ? (min.x + max.x) * 0.5f :
            Mathf.Clamp(position.x, min.x + halfWidth, max.x - halfWidth);
        position.y = max.y - min.y <= halfHeight * 2f ? (min.y + max.y) * 0.5f :
            Mathf.Clamp(position.y, min.y + halfHeight, max.y - halfHeight);
        return position;
    }

    private void SetPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, fixedZ);
    }
}
