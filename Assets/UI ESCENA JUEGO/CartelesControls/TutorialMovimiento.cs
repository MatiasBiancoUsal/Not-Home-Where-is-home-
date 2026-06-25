using UnityEngine;

public class TutorialMovimiento : MonoBehaviour
{
    public GameObject cartel;

    private bool tutorialActivo = false;
    private bool yaMostrado = false;

    private PlayerMovement movement;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!yaMostrado && collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            yaMostrado = true;
            tutorialActivo = true;
            cartel.SetActive(true);
        }
    }

    void Update()
    {
        if (tutorialActivo && movement.IsMoving)
        {
            cartel.SetActive(false);
            tutorialActivo = false;
        }
    }
}