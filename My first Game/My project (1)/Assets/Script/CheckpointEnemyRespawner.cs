using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CheckpointEnemyRespawner : MonoBehaviour
{
    private class Spawn
    {
        public GameObject template;
        public GameObject current;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public string name;
    }

    private readonly List<Spawn> spawns = new List<Spawn>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHooks() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == "StageEnemies_FirstPass" && candidate.gameObject.activeInHierarchy &&
                    candidate.GetComponent<CheckpointEnemyRespawner>() == null)
                    candidate.gameObject.AddComponent<CheckpointEnemyRespawner>();
    }

    private void Awake()
    {
        // sceneLoaded attaches this before Start/physics, while every placed enemy is pristine.
        var originals = new List<GameObject>();
        foreach (Transform child in transform)
            if (child.gameObject.activeSelf && child.GetComponent<EnemyHealth>() != null &&
                child.GetComponentInChildren<BossController>(true) == null &&
                (child.GetComponent<EnemyMeleeAI>() != null || child.GetComponent<EnemyRangedAI>() != null))
                originals.Add(child.gameObject);
        GameObject cache = new GameObject("RestSpawnTemplates");
        cache.transform.SetParent(transform, false);
        cache.SetActive(false);
        foreach (GameObject original in originals)
        {
            GameObject template = Instantiate(original, cache.transform, false);
            template.SetActive(false);
            spawns.Add(new Spawn
            {
                template = template, current = original, name = original.name,
                localPosition = original.transform.localPosition,
                localRotation = original.transform.localRotation,
                localScale = original.transform.localScale
            });
        }
    }

    public static void RespawnForRest(Scene scene)
    {
        foreach (EnemyProjectile projectile in Object.FindObjectsByType<EnemyProjectile>())
            if (projectile.gameObject.scene == scene && projectile.ResetOnCheckpointRest)
            {
                projectile.gameObject.SetActive(false);
                Object.Destroy(projectile.gameObject);
            }
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (CheckpointEnemyRespawner respawner in root.GetComponentsInChildren<CheckpointEnemyRespawner>())
                respawner.Respawn();
    }

    private void Respawn()
    {
        foreach (Spawn spawn in spawns)
        {
            if (spawn.current != null)
            {
                // Stop old attack coroutines and collision immediately, before deferred Destroy.
                spawn.current.SetActive(false);
                Destroy(spawn.current);
            }
            GameObject actor = Instantiate(spawn.template, transform, false);
            actor.name = spawn.name;
            actor.transform.localPosition = spawn.localPosition;
            actor.transform.localRotation = spawn.localRotation;
            actor.transform.localScale = spawn.localScale;
            spawn.current = actor;
            actor.SetActive(true);
        }
        Physics2D.SyncTransforms();
    }
}
