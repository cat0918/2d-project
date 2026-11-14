using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerDeathScreen : MonoBehaviour
{
    private PlayerHealth health;
    private bool restarting;
    private string error;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (health.DeathSequenceFinished && Input.GetKeyDown(KeyCode.R)) Restart();
    }

    // Temporary UI for testing the death loop before the final Canvas design.
    private void OnGUI()
    {
        if (!health.DeathSequenceFinished) return;
        float width = Mathf.Min(340f, Screen.width - 24f);
        float height = Mathf.Min(190f, Screen.height - 24f);
        Rect panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        GUI.Box(panel, "GAME OVER");
        bool wasEnabled = GUI.enabled;
        GUI.enabled = !restarting;
        if (GUI.Button(new Rect(panel.x + 20, panel.y + 55, width - 40, 48), "Restart (R)")) Restart();
        GUI.enabled = wasEnabled;
        if (!string.IsNullOrEmpty(error))
            GUI.Label(new Rect(panel.x + 15, panel.y + 115, width - 30, 65), error);
    }

    public void Restart()
    {
        if (!health.DeathSequenceFinished || restarting) return;
        string path = gameObject.scene.path;
        if (string.IsNullOrEmpty(path) || !Application.CanStreamedLevelBeLoaded(path))
        {
            error = "Add this scene to the active Build Profile scene list.";
            Debug.LogWarning(error, this);
            return;
        }
        restarting = true;
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
    }
}
