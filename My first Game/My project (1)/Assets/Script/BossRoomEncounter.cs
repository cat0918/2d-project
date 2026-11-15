using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class BossRoomEncounter : MonoBehaviour
{
    public BossController boss;
    public GameObject entranceGate;
    [Min(0f)] public float wakeDelay = 0.5f;
    [Min(0.1f)] public float wakeDuration = 1.5f;
    [Range(0.1f, 1f)] public float wakePlaybackSpeed = 0.6f;
    public bool alignBossFeet = true;
    [Min(0f)] public float combatDelay = 0.4f;
    private EnemyHealth health;
    private EnemyAnimation animationDriver;
    private EnemyContactDamage[] contacts;
    private bool[] contactStates;
    private bool ready;
    private bool started;
    private bool cleared;

    private void Awake()
    {
        SetGate(false);
        if (boss == null)
        {
            Debug.LogError("BossRoomEncounter needs a boss.", this);
            enabled = false;
            return;
        }
        health = boss.GetComponent<EnemyHealth>();
        animationDriver = boss.GetComponent<EnemyAnimation>();
        if (health == null || animationDriver == null)
        {
            Debug.LogError("Boss needs EnemyHealth and EnemyAnimation.", this);
            enabled = false;
            return;
        }
        boss.enabled = false;
        health.IsInvulnerable = true;
        contacts = boss.GetComponentsInChildren<EnemyContactDamage>(true);
        contactStates = new bool[contacts.Length];
        for (int i = 0; i < contacts.Length; i++)
        {
            contactStates[i] = contacts[i].enabled;
            contacts[i].enabled = false;
        }
    }

    private IEnumerator Start()
    {
        // EnemyAnimation initializes its override controller in Start.
        yield return null;
        if (alignBossFeet) animationDriver.AlignStandingFeetToCollider();
        if (!animationDriver.BeginSleepingPose())
            Debug.LogWarning("No valid sleeping pose; the boss will wait idle during its introduction.", this);
        ready = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!ready || started || boss == null || health.IsDead) return;
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead || other.isTrigger) return;
        Collider2D gateCollider = entranceGate != null ? entranceGate.GetComponent<Collider2D>() : null;
        // Wait until the entire player is inside; never close the gate through their body.
        if (gateCollider != null && other.bounds.max.x >= entranceGate.transform.position.x - 0.65f) return;
        started = true;
        boss.target = player.transform;
        SetGate(true);
        StartCoroutine(Wake(player));
    }

    private IEnumerator Wake(PlayerHealth player)
    {
        yield return new WaitForSeconds(wakeDelay);
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, wakeDuration) / Mathf.Clamp(wakePlaybackSpeed, 0.1f, 1f);
        while (elapsed < duration)
        {
            if (boss == null || player == null || player.IsDead)
            {
                SetGate(false);
                yield break;
            }
            elapsed += Time.deltaTime;
            animationDriver.SampleWakePose(elapsed / duration);
            yield return null;
        }
        animationDriver.FinishWakePose();
        yield return new WaitForSeconds(combatDelay);
        if (boss == null || player == null || player.IsDead) { SetGate(false); yield break; }
        health.IsInvulnerable = false;
        for (int i = 0; i < contacts.Length; i++)
            if (contacts[i] != null) contacts[i].enabled = contactStates[i];
        boss.enabled = true;
    }

    private void Update()
    {
        if (!started || cleared) return;
        if (boss == null || health == null || health.IsDead)
        {
            cleared = true;
            SetGate(false);
        }
        else if (boss.target != null)
        {
            PlayerHealth player = boss.target.GetComponent<PlayerHealth>();
            if (player != null && player.IsDead) SetGate(false);
        }
    }

    private void SetGate(bool closed)
    {
        if (entranceGate != null) entranceGate.SetActive(closed);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        SetGate(false);
    }
}
