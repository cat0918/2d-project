using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Session-only progress: survives death reloads, not quitting or a fresh Play session.
public static class RunCheckpointState
{
    private static string checkpointScene;
    private static string checkpointId;
    private static Vector3 checkpointPosition;
    private static string restartScene;
    private static bool restartPending;
    private static bool doubleJumpUnlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        SceneManager.sceneLoaded -= RestoreAfterRestart;
        checkpointScene = checkpointId = restartScene = null;
        checkpointPosition = Vector3.zero;
        restartPending = doubleJumpUnlocked = false;
    }

    public static void Save(Transform point)
    {
        if (point == null) return;
        checkpointScene = point.gameObject.scene.path;
        checkpointId = GetId(point);
        checkpointPosition = point.position;
    }

    public static void PrepareRestart(PlayerMove player, string scenePath)
    {
        restartScene = scenePath;
        doubleJumpUnlocked = player != null && player.HasDoubleJump;
        restartPending = true;
        SceneManager.sceneLoaded -= RestoreAfterRestart;
        SceneManager.sceneLoaded += RestoreAfterRestart;
    }

    public static void CancelRestart()
    {
        restartPending = false;
        SceneManager.sceneLoaded -= RestoreAfterRestart;
    }

    private static void RestoreAfterRestart(Scene scene, LoadSceneMode mode)
    {
        if (!restartPending || scene.path != restartScene) return;
        CancelRestart();
        PlayerMove player = null;
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
            foreach (PlayerMove candidate in root.GetComponentsInChildren<PlayerMove>())
                if (candidate.isActiveAndEnabled) { player = candidate; break; }
        if (player == null)
        {
            Debug.LogWarning("Restart loaded but no active player was found.");
            return;
        }
        // sceneLoaded runs after Awake and before Start, so the camera starts at the restored player.
        if (doubleJumpUnlocked)
        {
            player.UnlockDoubleJump();
            foreach (GameObject root in roots)
                foreach (DoubleJumpPickup pickup in root.GetComponentsInChildren<DoubleJumpPickup>())
                    pickup.gameObject.SetActive(false);
        }
        if (checkpointId == null || checkpointScene != scene.path) return;
        Vector3 destination = checkpointPosition;
        Stage2Checkpoint found = null;
        foreach (GameObject root in roots)
            foreach (Stage2Checkpoint point in root.GetComponentsInChildren<Stage2Checkpoint>())
                if (point.isActiveAndEnabled && point.respawnPoint != null && GetId(point.respawnPoint) == checkpointId)
                { found = point; break; }
        if (found != null && found.course != null)
        {
            destination = found.respawnPoint.position;
            found.course.SetCheckpoint(found.respawnPoint);
        }
        else
            Debug.LogWarning("Saved checkpoint object was not found; restoring its saved position.");
        player.transform.position = destination;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = destination;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
        }
        Physics2D.SyncTransforms();
    }

    private static string GetId(Transform point)
    {
        var parts = new Stack<string>();
        for (Transform t = point; t != null; t = t.parent)
            parts.Push(t.name + "#" + t.GetSiblingIndex());
        return string.Join("/", parts);
    }
}
