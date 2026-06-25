using UnityEngine;
using System.Collections;

public class TutorialTrigger : MonoBehaviour
{
    public GameObject cartel;
    public float tiempoVisible = 5f;

    private bool activado = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("TRIGGER ACTIVADO");

        if (!activado && other.CompareTag("Player"))
        {
            activado = true;
            StartCoroutine(MostrarCartel());
            GetComponent<Collider2D>().enabled = false;
        }
    }

    IEnumerator MostrarCartel()
{
    Debug.Log("Mostrando cartel");

    cartel.SetActive(true);

    yield return new WaitForSeconds(5);

    cartel.SetActive(false);
}
}