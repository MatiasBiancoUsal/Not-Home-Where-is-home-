using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    public Image[] hearts;   // 5 imágenes de corazón
    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;

    private HealthHandler playerHealth;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<HealthHandler>();
            playerHealth.OnHealthChanged += UpdateHearts;
            UpdateHearts(playerHealth.CurrentHealth);
        }
    }

    public void UpdateHearts(int health)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            int value = health - (i * 2);

            if (value >= 2)
                hearts[i].sprite = fullHeart;
            else if (value == 1)
                hearts[i].sprite = halfHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHearts;
    }
}