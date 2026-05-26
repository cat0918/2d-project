using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Image[] heartImages;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHearts;
            UpdateHearts(playerHealth.CurrentHealth, playerHealth.maxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHearts;
        }
    }

    private void UpdateHearts(int currentHealth, int maxHealth)
    {
        int visibleCount = Mathf.Min(heartImages.Length, maxHealth);

        for (int i = 0; i < heartImages.Length; i++)
        {
            bool shouldShow = i < visibleCount;
            heartImages[i].enabled = shouldShow;
            if (!shouldShow) continue;

            heartImages[i].sprite = i < currentHealth ? fullHeartSprite : emptyHeartSprite;
        }
    }
}
