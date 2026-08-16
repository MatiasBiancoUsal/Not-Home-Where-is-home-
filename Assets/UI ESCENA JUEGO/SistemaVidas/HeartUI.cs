using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    public Image[] hearts;   // 5 im�genes de coraz�n
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

            // Si el objeto con el tag "Player" NO es la niña (paso: un cuadrado del piso
            // quedo tagueado como Player en Zona 4), avisamos en vez de tirar un
            // NullReferenceException suelto que no se entiende de donde viene.
            if (playerHealth == null)
            {
                Debug.LogWarning("HeartUI: el objeto con el tag 'Player' de esta escena es '" + playerObj.name +
                                 "', y no tiene HealthHandler. Seguramente hay otro objeto tagueado como Player " +
                                 "por error. Los corazones no se van a actualizar.", playerObj);
                return;
            }

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